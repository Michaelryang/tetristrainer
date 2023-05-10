using UnityEngine;
using UnityEngine.Tilemaps;

public class Piece : MonoBehaviour
{
    public Board board { get; private set; }
    public TetrominoData data { get; private set; }
    public Vector3Int position { get; private set; }
    public Vector3Int[] cells { get; private set; }
    public int rotationIndex { get; private set; }

    public float stepDelay = 1f;
    public float lockDelay = 0.5f;

    private float stepTime;
    private float lockTime;


    // handling settings
    public float handlingARR;
    public float handlingDAS;
    public float handlingSDF;
    public float DASTime;
    public float ARRTime;
    public float SDFTime;

    private bool bActiveDASRight;
    private bool bActiveDASLeft;
    private bool usedHold;


    // sfx

    public AudioSource rotateSFX;
    public AudioSource moveSFX;
    public AudioSource[] harddropSFX;

    public void Initialize(Board b, Vector3Int pos, TetrominoData d)
    {
        //handlingARR = 0;
        //handlingDAS = 0.1f;
        //handlingSDF = 0.01f;

        bActiveDASRight = false;
        bActiveDASLeft = false;
        board = b;
        position = pos;
        data = d;
        rotationIndex = 0;
        SetStepTime();
        DASTime = 0f;
        ARRTime = 0f;
        SDFTime = 0f;
        lockTime = 0f;

        if (this.cells == null)
        {
            this.cells = new Vector3Int[data.cells.Length];

        }

        for (int x = 0; x < data.cells.Length; ++x)
        {
            this.cells[x] = (Vector3Int)data.cells[x];
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void SetStepTime()
    {
        stepTime = Time.time + stepDelay * this.board.GetGravityMultiplier();
    }

    // Update is called once per frame
    private void Update()
    {
        if (this.board.mode == Mode.Sprint && !this.board.sprintStarted)
        {
            return;
        }


        this.board.Clear(this);

        this.lockTime += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Rotate(1);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            Rotate(-1);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (Move(Vector2Int.left))
            {
                moveSFX.PlayOneShot(moveSFX.clip);
            }
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (Move(Vector2Int.right))
            {
                moveSFX.PlayOneShot(moveSFX.clip);
            }
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Move(Vector2Int.down);
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            HardDrop();
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            Hold();
        }
        if ( Time.time >= this.stepTime)
        {
            Step();
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            if (!bActiveDASRight)
            {
                if (!bActiveDASLeft)
                {
                    bActiveDASLeft = true;
                }
                else
                {
                    DASTime += Time.deltaTime;

                    if (DASTime > handlingDAS)
                    {
                        if (handlingARR == 0f)
                        {
                            while (Move(Vector2Int.left))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            ARRTime += Time.deltaTime;
                            if (ARRTime > handlingARR)
                            {
                                Move(Vector2Int.left);
                                ARRTime = 0f;
                            }
                        }

                    }
                }
            }
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            if (!bActiveDASLeft)
            {
                if (!bActiveDASRight)
                {
                    bActiveDASRight = true;
                }
                else
                {
                    DASTime += Time.deltaTime;

                    if (DASTime > handlingDAS)
                    {
                        if (handlingARR == 0f)
                        {
                            while (Move(Vector2Int.right))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            ARRTime += Time.deltaTime;
                            if (ARRTime > handlingARR)
                            {
                                Move(Vector2Int.right);
                                ARRTime = 0f;
                            }
                        }

                    }
                }
            }
            
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            SDFTime += Time.deltaTime;
            if ( SDFTime > handlingSDF )
            {
                SDFTime = 0;
                Move(Vector2Int.down);
            }
        }

        if ( Input.GetKeyUp(KeyCode.LeftArrow))
        {
            bActiveDASLeft = false;
            DASTime = 0;
        }
        if ( Input.GetKeyUp(KeyCode.RightArrow))
        {
            bActiveDASRight = false;
            DASTime = 0;
        }

        this.board.Set(this);
    }

    private void Step()
    {
        SetStepTime();
        Move(Vector2Int.down);

        if ( this.lockTime >= lockDelay) 
        {
            Lock();
        }
    }

    private void HardDrop()
    {
        harddropSFX[0].PlayOneShot(harddropSFX[0].clip);

        while (Move(Vector2Int.down))
        {
            continue;
        }

        Lock();
    }

    private void Lock()
    {
        usedHold = false;
        this.board.Set(this);

        this.board.ClearLines();

        this.board.SpawnPiece();
    }
    
    private bool Move(Vector2Int translation)
    {
        Vector3Int newPos = position;
        newPos.x += translation.x;
        newPos.y += translation.y;

        bool validPos = this.board.IsValidPosition(this, newPos);

        if (validPos)
        {
           
            this.position = newPos;
            this.lockTime = 0f;
        }

        return validPos;
    }

    private void Rotate(int direction)
    {
        int originalRotation = this.rotationIndex;
        this.rotationIndex = Wrap(this.rotationIndex + direction, 0, 4);

        ApplyRotationMatrix(direction);

        if (!TestWallKicks(rotationIndex, direction))
        {
            rotationIndex = originalRotation;
            ApplyRotationMatrix(-direction);
        }
        else 
        {
            rotateSFX.PlayOneShot(rotateSFX.clip);
        }
    }

    private void ApplyRotationMatrix(int direction)
    {
        for (int i = 0; i < this.cells.Length; ++i)
        {
            Vector3 cell = this.cells[i];

            int x, y;

            switch (this.data.tetromino)
            {
                case Tetromino.I:
                case Tetromino.O:
                    cell.x -= 0.5f;
                    cell.y -= 0.5f;
                    x = Mathf.CeilToInt((cell.x * Data.RotationMatrix[0] * direction) + (cell.y * Data.RotationMatrix[1] * direction));
                    y = Mathf.CeilToInt((cell.x * Data.RotationMatrix[2] * direction) + (cell.y * Data.RotationMatrix[3] * direction));
                    break;
                default:
                    x = Mathf.RoundToInt((cell.x * Data.RotationMatrix[0] * direction) + (cell.y * Data.RotationMatrix[1] * direction));
                    y = Mathf.RoundToInt((cell.x * Data.RotationMatrix[2] * direction) + (cell.y * Data.RotationMatrix[3] * direction));
                    break;

            }
            this.cells[i] = new Vector3Int(x, y, 0);
        }

    }

    private bool TestWallKicks(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = GetWallKickIndex(rotationIndex, rotationDirection);

        for ( int i = 0; i < this.data.wallKicks.GetLength(1); ++i)
        {
            Vector2Int translation = this.data.wallKicks[wallKickIndex, i];

            if ( Move(translation))
            {

                return true;
            }
        }

        return false;
    }

    private int GetWallKickIndex(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = rotationIndex * 2;
        if ( rotationDirection < 0)
        {
            --wallKickIndex;

        }

        return Wrap(wallKickIndex, 0, this.data.wallKicks.GetLength(0));
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

    private void Hold()
    {
        if (!usedHold)
        {
            usedHold = true;

            board.HoldPiece();
        }
    }
}
