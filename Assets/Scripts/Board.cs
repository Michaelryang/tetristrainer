using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
public enum Mode
{ 
    Zen,
    Sprint,
    Cheese,
    Campaign,
}


public class Board : MonoBehaviour
{
    public Tilemap tilemap { get; private set; }
    public Piece activePiece { get; private set; }
    private int activePieceIndex;
    public Piece heldPiece { get; private set; }
    private int heldPieceIndex;
    public TetrominoData[] tetrominoes;
    public Tile garbageTile;

    public Vector3Int spawnPosition;
    public Vector2Int boardSize;//= new Vector2Int(10, 24);
    public int queueLength;

    private List<int> queue;
    private int[] bag = new int[7];
    private int bagIndex;

    public SpriteRenderer holdPieceRenderer;
    public SpriteRenderer[] queuePieceRenderer;
    public List<Sprite> pieceSprites;


    // sfx

    public AudioSource[] clearSFX;
    public AudioSource gameOverSFX;
    public AudioSource restartSFX;
    public AudioSource countdownSFX;

    public AudioSource[] backgroundMusic;

    private float restartDelay = 1.0f;
    private float restartTime;

    public Mode mode;
    public GameObject buttonStartSprint;

    public Text modeText;
    
    public bool sprintStarted;
    private bool sprintCountdownStarted;
    private float sprintCountdown;
    public Text sprintCountdownText;
    public Text sprintTimerText;
    public Text sprintLinesClearedText;
    private float sprintTimer;
    public int sprintLinesCleared;



    public int numCheeseLines;

    // campaign
    public int campaignLevel;
    public Text campaignText;
    private int campaignLevelObjective; // 0 survive (cheese), 1 clear, 2, survive (speed)
    private int campaignLinesToClear;
    private bool levelFinished;


    private float campaignGravityMultiplier;
    public float cheeseSpawnDelay;
    private float cheeseSpawnTimer;

    private int campaignClearDensity;
    private bool[] campaignClearRows = new bool[10];
    
    public RectInt Bounds
    {
        get
        {
            Vector2Int pos = new Vector2Int(-this.boardSize.x / 2, -(this.boardSize.y) / 2 + 1);
            return new RectInt(pos, boardSize);
        }

    }

    private void Awake()
    {
        this.tilemap = GetComponentInChildren<Tilemap>();
        this.activePiece = GetComponentInChildren<Piece>();
        for (int x = 0; x < this.tetrominoes.Length; ++x)
        {
            tetrominoes[x].Initialize();
        }
    }
    private void Update()
    {
        if (mode == Mode.Sprint)
        {
            if (sprintCountdownStarted)
            {
                if (sprintCountdown > 0.0f)
                {
                    sprintCountdownText.text = ((int)sprintCountdown + 1).ToString();
                    sprintCountdown -= Time.deltaTime;
                }
                else
                {
                    if (sprintCountdown < -1.0f)
                    {
                        sprintCountdownStarted = false;

                        sprintCountdownText.text = "";
                    }
                    else
                    {
                        sprintCountdown -= Time.deltaTime;
                        sprintCountdownText.text = "START";
                        sprintStarted = true;
                    }
                }
            }

            if (sprintStarted)
            {
                sprintTimer += Time.deltaTime;
                sprintTimerText.text = "Time: " + sprintTimer;
            }
        }
        else if (mode == Mode.Campaign)
        {
            CampaignLevelUp();

            if (campaignLevelObjective == 0)
            {
                cheeseSpawnTimer += Time.deltaTime;
                if (cheeseSpawnTimer > cheeseSpawnDelay)
                {
                    cheeseSpawnTimer = 0.0f;
                    SpawnGarbageLine();
                }
            }
            
        }

        if (Input.GetKey(KeyCode.R))
        {
            restartTime += Time.deltaTime;

            if (restartTime > restartDelay)
            {
                restartSFX.PlayOneShot(restartSFX.clip);
                Reset();

                restartTime = -3.0f;
            }
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            restartTime = 0.0f;
        }
    }

    private void CampaignLevelUp()
    {
        if (levelFinished)
        {
            levelFinished = false;
            Reset();
            ++campaignLevel;

            campaignLevelObjective = campaignLevel % 3;

            campaignText.text = "Level " + (campaignLevel) + ": ";

            if (campaignLevelObjective % 3 == 0) // cheese
            {
                campaignLinesToClear = (campaignLevel / 3) * 5;

                campaignText.text += "Clear " + campaignLinesToClear + " more cheese!";

                cheeseSpawnDelay *= 0.9f;

                for (int x = 0; x < 2; ++x)
                {
                    SpawnGarbageLine();
                }
            }
            else if (campaignLevel % 3 == 1) // clear
            {
                campaignLinesToClear = 5 + campaignLevel / 3;

                if (campaignLinesToClear > 10)
                {
                    campaignLinesToClear = 10;
                }

                for (int x = 0; x < campaignClearRows.Length; ++x)
                {
                    campaignClearRows[x] = true;
                }

                for (int x = 0; x < campaignLinesToClear; ++x)
                {
                    campaignClearRows[x] = false;
                }

                campaignClearDensity = Random.Range(6, 8);

                int mono = -1;
                if (Random.Range(0, 2) == 0)
                {
                    mono = Random.Range(0, tetrominoes.Length);
                }

                for (int x = 0; x < campaignLinesToClear; ++x)
                {
                    

                    SpawnPieceLine(Wrap(campaignClearDensity + Random.Range(-1,3), 1, 9), mono, campaignLevel / 3 % 3 == 1);
                }
                
                campaignText.text += "Clear these " + campaignLinesToClear + " lines!";
            }
            else // gravity
            {
                campaignLinesToClear = 5 + (campaignLevel / 3) * 5;

                campaignGravityMultiplier = Mathf.Pow( 0.8f, (campaignLevel / 3) + 3);

                campaignText.text += "Survive " + campaignLinesToClear + " more lines!";
            }
        }
    }
    private void Reset()
    {
        this.tilemap.ClearAllTiles();
        SetRandomBag();
        for (int x = 0; x < queueLength; ++x)
        {
            queue[x] = bag[bagIndex];
            ++bagIndex;
        }
        SpawnPiece();
        holdPieceRenderer.sprite = null;
        heldPieceIndex = -1;
        heldPiece = null;

        if (mode == Mode.Cheese)
        {
            for (int x = 0; x < numCheeseLines; ++x)
            {
                SpawnGarbageLine();
            }
        }
    }
    private void Start()
    {
        SetModeZen();
        for (int x = 0; x < backgroundMusic.Length; ++x)
        {
            backgroundMusic[x].Stop();
        }
        backgroundMusic[(int)mode].Play();

        queue = new List<int>();

        for (int x = 0; x < queueLength; ++x)
        {
            queue.Add(-1);
        }
        SetRandomBag();
        for (int x = 0; x < queueLength; ++x)
        {
            queue[x] = bag[bagIndex];
            ++bagIndex;
        }
        SpawnPiece();
    }

    private void UpdateQueueDisplay()
    {
        for (int x = 0; x < queuePieceRenderer.Length; ++x)
        {
            queuePieceRenderer[x].sprite = pieceSprites[queue[x]];
            queuePieceRenderer[x].color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        }
    }

    public void SpawnPiece()
    {
        if (bagIndex == tetrominoes.Length)
        {
            SetRandomBag();
        }

        int newMino = queue[0];
        for (int x = 1; x < queueLength; ++x)
        {
            queue[x - 1] = queue[x];
        }
        queue[queueLength - 1] = bag[bagIndex];
        ++bagIndex;

        TetrominoData data = this.tetrominoes[newMino];

        activePieceIndex = newMino;
        this.activePiece.Initialize(this, this.spawnPosition, data);

        UpdateQueueDisplay();

        if (IsValidPosition(this.activePiece, this.spawnPosition))
        {
            Set(this.activePiece);
        }
        else
        {
            GameOver();
        }
    }

    public void HoldPiece()
    {
        if (heldPiece == null)
        {
            heldPiece = this.activePiece;
            heldPieceIndex = activePieceIndex;
            SpawnPiece();
        }
        else
        {
            Piece temp = this.activePiece;
            int tempIndex = activePieceIndex;
            activePieceIndex = heldPieceIndex;
            heldPieceIndex = tempIndex;

            this.activePiece = heldPiece;
            heldPiece = this.activePiece;

            TetrominoData data = this.tetrominoes[activePieceIndex];

            this.activePiece.Initialize(this, this.spawnPosition, data);

            if (IsValidPosition(this.activePiece, this.spawnPosition))
            {
                Set(this.activePiece);
            }
            else
            {
                GameOver();
            }


        }

        holdPieceRenderer.sprite = pieceSprites[heldPieceIndex];
        holdPieceRenderer.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    }

    private void SetRandomBag()
    {
        for (int x = 0; x < tetrominoes.Length; ++x)
        {
            bag[x] = x;
        }

        for (int x = 0; x < tetrominoes.Length; ++x)
        {
            int y = Random.Range(0, tetrominoes.Length - 1);

            int t;
            t = bag[x];
            bag[x] = bag[y];
            bag[y] = t;
        }

        bagIndex = 0;
    }

    private void GameOver()
    {
        gameOverSFX.PlayOneShot(gameOverSFX.clip);
        this.tilemap.ClearAllTiles();

        if (mode == Mode.Cheese)
        {
            for (int x = 0; x < numCheeseLines; ++x)
            {
                SpawnGarbageLine();
            }
        }
    }

    public void Set(Piece piece)
    {
        for (int x = 0; x < piece.cells.Length; ++x)
        {
            Vector3Int tilePosition = piece.cells[x] + piece.position;
            this.tilemap.SetTile(tilePosition, piece.data.tile);
        }
    }

    public void Clear(Piece piece)
    {
        for (int x = 0; x < piece.cells.Length; ++x)
        {
            Vector3Int tilePosition = piece.cells[x] + piece.position;
            this.tilemap.SetTile(tilePosition, null);
        }
    }

    public bool IsValidPosition(Piece piece, Vector3Int position)
    {
        RectInt bounds = this.Bounds;

        for (int x = 0; x < piece.cells.Length; ++x)
        {
            Vector3Int tilePosition = piece.cells[x] + position;
            if (this.tilemap.HasTile(tilePosition))
            {
                return false;
            }
            else if (!bounds.Contains((Vector2Int)tilePosition))
            {
                return false;
            }

        }

        return true;
    }

    public void ClearLines()
    {
        RectInt bounds = this.Bounds;
        int row = bounds.yMin;
        int numrows = 0;

        while (row < bounds.yMax)
        {
            if (isLineFull(row))
            {

                if (mode == Mode.Cheese && IsLineGarbage(row))
                {
                    LineClear(row);
                    SpawnGarbageLine();
                }
                else if (mode == Mode.Campaign)
                {
                    if (campaignLevelObjective == 1)
                    {
                        if (row >= bounds.yMin && row < bounds.yMin + 10)
                        {
                            if (campaignClearRows[row + 10] == false)
                            {
                                campaignClearRows[row + 10] = true;

                                for (int y = row + 1; y < bounds.yMin + 10; ++y)
                                {
                                    campaignClearRows[y - 1 + 10] = campaignClearRows[y + 10];
                                }

                                campaignClearRows[campaignClearRows.Length - 1] = true;

                                --campaignLinesToClear;

                                if (campaignLinesToClear <= 0)
                                {
                                    levelFinished = true;
                                }

                                campaignText.text = "Level " + (campaignLevel) + ": ";
                                campaignText.text += "Clear these " + campaignLinesToClear + " lines!";
                            }

                            LineClear(row);
                        }
                        else
                        {
                            LineClear(row);
                        }

                    }
                    else if (campaignLevelObjective == 2)
                    {
                        --campaignLinesToClear;

                        if (campaignLinesToClear <= 0)
                        {
                            levelFinished = true;
                        }

                        campaignText.text = "Level " + (campaignLevel) + ": ";
                        campaignText.text += "Survive " + campaignLinesToClear + " more lines!";

                        LineClear(row);

                    }
                    else
                    {
                        if (IsLineGarbage(row))
                        {
                            --campaignLinesToClear;

                            if (campaignLinesToClear <= 0)
                            {
                                levelFinished = true;
                            }

                            campaignText.text = "Level " + (campaignLevel) + ": ";
                            campaignText.text += "Clear " + campaignLinesToClear + " more cheese!";
                        }

                        LineClear(row);
                    }
                }
                else
                {
                    LineClear(row);
                }

                ++numrows;
            }
            else
            {
                ++row;
            }
        }

        if (numrows > 0)
        {
            if (mode == Mode.Sprint && sprintStarted)
            {
                sprintLinesCleared += numrows;

                sprintLinesClearedText.text = "Lines Cleared: " + sprintLinesCleared;

                if (sprintLinesCleared >= 40)
                {
                    sprintStarted = false;
                }
            }

            clearSFX[numrows - 1].PlayOneShot(clearSFX[numrows - 1].clip);
        }
    }

    private bool isLineFull(int row)
    {
        RectInt bounds = this.Bounds;

        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);

            if (!this.tilemap.HasTile(position))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsLineGarbage(int row)
    {
        RectInt bounds = this.Bounds;

        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);

            if (this.tilemap.GetTile(position) == garbageTile)
            {
                return true;
            }
        }

        return false;
    }

    private void LineClear(int row)
    {
        RectInt bounds = this.Bounds;

        for ( int col = bounds.xMin; col < bounds.xMax; ++col)
        {
            Vector3Int position = new Vector3Int(col,row,0);
            this.tilemap.SetTile(position, null);
        }

        while (row < bounds.yMax)
        {
            for ( int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int position = new Vector3Int(col, row + 1, 0);
                TileBase above = this.tilemap.GetTile(position);

                position = new Vector3Int(col, row, 0);
                this.tilemap.SetTile(position, above);
            }

            ++row;
        }
    }

    public void SetModeZen()
    {
        if (mode != Mode.Zen)
        {
            
            mode = Mode.Zen;
            DeactivateSprintUI();
            DeactivateCampaignUI();
            modeText.text = "Mode: Zen";
            Reset();

            for (int x = 0; x < backgroundMusic.Length; ++x)
            {
                backgroundMusic[x].Stop();
            }
            backgroundMusic[(int)mode].Play();

            Reset();
        }
    }

    public void SetModeSprint()
    {
        

        if (mode != Mode.Sprint)
        {
            sprintTimer = 0.0f;
            sprintLinesCleared = 0;
            sprintTimerText.text = "Time: " + sprintTimer;
            sprintLinesClearedText.text = "Lines Cleared: " + sprintLinesCleared;
            sprintStarted = false;
            sprintCountdownStarted = false;
            mode = Mode.Sprint;
            Reset();
            modeText.text = "Mode: Sprint";
            DeactivateCampaignUI();
            buttonStartSprint.SetActive(true);

            for (int x = 0; x < backgroundMusic.Length; ++x)
            {
                backgroundMusic[x].Stop();
            }
            backgroundMusic[(int)mode].Play();
        }
    }

    public void SetModeCheese()
    {
        if (mode != Mode.Cheese)
        {
            Reset();
            mode = Mode.Cheese;

            modeText.text = "Mode: Cheese";

            DeactivateSprintUI();
            DeactivateCampaignUI();
            for (int x = 0; x < backgroundMusic.Length; ++x)
            {
                backgroundMusic[x].Stop();
            }
            backgroundMusic[(int)mode].Play();

            for (int x = 0; x < numCheeseLines; ++x)
            {
                SpawnGarbageLine();
            }
        }
        
    }

    private void SpawnPieceLine(int density, int mono, bool rainbow)
    {
        RectInt bounds = this.Bounds;
        int row = bounds.yMax - 1;
        while (row > bounds.yMin)
        {
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int position = new Vector3Int(col, row - 1, 0);
                TileBase below = this.tilemap.GetTile(position);

                position = new Vector3Int(col, row, 0);
                this.tilemap.SetTile(position, below);
            }

            --row;
        }

        Tile rainbowTile = tetrominoes[Random.Range(0, tetrominoes.Length)].tile;

        for (int col = bounds.xMin; col < bounds.xMax; ++col)
        {
            Vector3Int position = new Vector3Int(col, bounds.yMin, 0);

            if (mono >= 0)
            {
                this.tilemap.SetTile(position, tetrominoes[mono].tile);
            }
            else
            {
                if (rainbow)
                {
                    this.tilemap.SetTile(position, rainbowTile);
                }
                else 
                {
                    this.tilemap.SetTile(position, tetrominoes[Random.Range(0, tetrominoes.Length)].tile);
                }
                
            }
        }

        for (int x = 0; x < 10 - density; ++x)
        {
            int currentX = Random.Range(bounds.xMin, bounds.xMax);

            int count = Random.Range(1, 50);

            while ( count > 0 )
            {
                currentX = Wrap(currentX + 1, bounds.xMin, bounds.xMax);

                if (this.tilemap.GetTile(new Vector3Int(currentX, bounds.yMin, 0)) != null)
                {
                    --count;
                }
            }

            Vector3Int emptyposition = new Vector3Int(currentX, bounds.yMin, 0);
            this.tilemap.SetTile(emptyposition, null);
        }
        
    }

    private void SpawnGarbageLine()
    {
        RectInt bounds = this.Bounds;
        int row = bounds.yMax - 1;
        while (row > bounds.yMin)
        {
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int position = new Vector3Int(col, row - 1, 0);
                TileBase below = this.tilemap.GetTile(position);

                if (mode == Mode.Campaign && campaignLevelObjective == 0)
                {
                    if (this.tilemap.GetTile(position) != null && !IsActivePiecePosition(position) && IsActivePiecePosition(new Vector3Int(col, row, 0)))
                    {
                        GameOver();
                    }

                    if (!IsActivePiecePosition(position))
                    {
                        position = new Vector3Int(col, row, 0);
                        this.tilemap.SetTile(position, below);
                    }
                }
                else
                {
                    position = new Vector3Int(col, row, 0);
                    this.tilemap.SetTile(position, below);
                }
            }

            --row;
        }

        for (int col = bounds.xMin; col < bounds.xMax; ++col)
        {
            Vector3Int position = new Vector3Int(col, bounds.yMin, 0);
            this.tilemap.SetTile(position, garbageTile);
        }

        int emptyspot = Random.Range(bounds.xMin, bounds.xMax);
        Vector3Int emptyposition = new Vector3Int(emptyspot, bounds.yMin, 0);
        this.tilemap.SetTile(emptyposition, null);
    }

    private bool IsActivePiecePosition(Vector3Int position)
    {
        for (int x = 0; x < 4; ++x)
        {
            if (activePiece.cells[x] + activePiece.position == position)
            {
                return true;
            }
        }

        return false;
    }

    public void SetModeCampaign()
    {
        if (mode != Mode.Campaign)
        {
            Reset();
            mode = Mode.Campaign;
            modeText.text = "Mode: Campaign";

            DeactivateSprintUI();


            levelFinished = true;
            for (int x = 0; x < backgroundMusic.Length; ++x)
            {
                backgroundMusic[x].Stop();
            }
            backgroundMusic[(int)mode].Play();
        }
        
    }

    public void StartSprintCountdown()
    {
        countdownSFX.PlayOneShot(countdownSFX.clip);
        Reset();
        sprintCountdown = 3.0f;
        sprintCountdownStarted = true;
        sprintStarted = false;
        sprintTimer = 0.0f;
        sprintLinesCleared = 0;

        sprintTimerText.text = "Time: " + sprintTimer;
        sprintLinesClearedText.text = "Lines Cleared: " + sprintLinesCleared;
    }

    void DeactivateSprintUI()
    {
        buttonStartSprint.SetActive(false);
        sprintCountdownText.text = "";
        sprintTimerText.text = "";
        sprintLinesClearedText.text = "";
    }

    void DeactivateCampaignUI()
    {
        campaignText.text = "";
        campaignLevel = 0;
    }

    private int Wrap(int input, int min, int max)
    {
        if (input < min)
        {
            return max - (min - input) % (max - min);
        }
        else
        {
            return min + (input - min) % (max - min);
        }
    }

    public float GetGravityMultiplier()
    {
        if (mode == Mode.Campaign && campaignLevelObjective == 2)
        {
            return campaignGravityMultiplier;
        }
        else
        {
            return 1.0f;
        }
    }
}
