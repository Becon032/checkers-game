using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public GameObject[,] pieces = new GameObject[8, 8];

    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;

    public GameObject currentPlayerIden;
    public GameObject winPlayerIden;

    public GameObject DeadPiecesSpawn;
    public float spawnRange = 1f;
    public bool isAiPlaying = false;

    public Material WhiteActiveMat;
    public Material BlackActiveMat;
    public Material WhiteMat;
    public Material BlackMat;


    GameObject selectedPiece;

    private List<GameObject> forcedPieces = new List<GameObject>();
    private List<GameObject> moveablePieces = new List<GameObject>();

    private List<GameObject> AiSafeMovePieces = new List<GameObject>();

    Vector3 boardOffSet = new Vector3(-4f, 0f, -4f);
    Vector3 pieceOffSet = new Vector3(0.5f, 0f, 0.5f);

    Vector2 mouseOver;
    Vector2 startDrag = new Vector2(-1, -1);
    Vector2 endDrag;

    public bool isWhiteTurn = true;
    bool hasKilled = false;
    Vector2 hasKilledPiece;


    private void Start() {
        GenerateBoard();
    }
    private void Update() {
        MouseOverUpdate();

        int x = (int)mouseOver.x;
        int y = (int)mouseOver.y;

        if (selectedPiece != null) {
            UpdatePieceDrag(selectedPiece);
        }

        if (Input.GetMouseButtonDown(0)) {
            startDrag = mouseOver;
            SelectPiece(x, y);
            //Debug.Log("selectPiece()");
        }

        if (Input.GetMouseButtonUp(0)) {
            endDrag = mouseOver;
            TryMove((int)startDrag.x, (int)startDrag.y, (int)endDrag.x, (int)endDrag.y);
            //Debug.Log("end:" + endDrag);
        }
    }

    private void GenerateBoard() {
        //Generate white pieces
        for (int y = 0; y < 3; y++) {
            bool oddRow = (y % 2 == 0);
            for (int x = 0; x < 8; x += 2) {
                GeneratePiece((oddRow) ? x : x+1, y, true);
            }
        }

        //Generate black pieces
        for (int y = 5; y < 8; y++) {
            bool oddRow = (y % 2 == 0);
            for (int x = 0; x < 8; x += 2) {
                GeneratePiece((oddRow) ? x : x + 1, y, false);
            }
        }

        moveablePieces = ScanForMoveable();
        HighlightMoveable();
    }
    private void GeneratePiece(int x, int y, bool isWhite) {
        //Debug.Log("Generated piece:" + y + " " + x);
        GameObject go = Instantiate((isWhite) ? whitePiecePrefab : blackPiecePrefab);
        go.transform.SetParent(transform);
        go.name = (isWhite) ? "White Piece" : "Black Piece";
        pieces[x, y] = go;
        MovePiece(go, x, y, x, y);
    }

    private void MovePiece(GameObject p, int x1, int y1, int x2, int y2) {
        Vector3 position = new Vector3(x2 + boardOffSet.x + pieceOffSet.x, 0.1f, y2 + boardOffSet.z + pieceOffSet.z);
        p.transform.position = position;
        
        if (x1 == x2 && y1 == y2) {
            pieces[x2, y2] = p;
        } else {
            pieces[x2, y2] = p;
            pieces[x1, y1] = null;
        }

        CheckForKing(p, y2);

    }

    private void MouseOverUpdate() {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 25f, LayerMask.GetMask("Board"))) {
            mouseOver.x = (int)(hit.point.x - boardOffSet.x);
            mouseOver.y = (int)(hit.point.z - boardOffSet.z);
        } else {
            mouseOver.x = -1;
            mouseOver.y = -1;
        }
    }  
    private void UpdatePieceDrag(GameObject p) {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 25f, LayerMask.GetMask("Board"))) {

            if (moveablePieces.Contains(selectedPiece)) {
                p.transform.position = hit.point + Vector3.up * 0.5f;
            }

        }
    }


    private void SelectPiece(int x, int y) {
        if (x < 0 || y < 0 || x > 7 || y > 7) {
            Debug.Log("Point is not on board");
            return;
        }

        GameObject p = pieces[x, y];
        //Debug.Log(p.name);
        if (p != null) {
            selectedPiece = p;
        }
        
        
    }
    private void TryMove(int x1, int y1, int x2, int y2) {


        //If is out the board or If is not moved
        if (x2 < 0 || y2 < 0 || x1 == x2 && y1 == y2) {

            if (selectedPiece != null) 
                MovePiece(selectedPiece, x1, y1, x1, y1);

            startDrag = new Vector2(-1, -1);
            selectedPiece = null;
            return;
        }

        //If moved in wrong turn
        if (selectedPiece != null) {
            if (!isWhiteTurn && selectedPiece.name == "White Piece" || !isWhiteTurn && selectedPiece.name == "White Piece King") {
                startDrag = new Vector2(-1, -1);
                selectedPiece = null;
                return;
            }
            if (isWhiteTurn && selectedPiece.name == "Black Piece" || isWhiteTurn && selectedPiece.name == "Black Piece King") {
                startDrag = new Vector2(-1, -1);
                selectedPiece = null;
                return;
            }
        }



        //is a valid move
        if (IsValidMove(x1, y1, x2, y2, selectedPiece)) {

            if (forcedPieces.Count != 0 && !hasKilled) {

                if (selectedPiece != null)
                    MovePiece(selectedPiece, x1, y1, x1, y1);

                startDrag = new Vector2(-1, -1);
                selectedPiece = null;
                Debug.Log("You must kill!");
                return;
            }



            if (selectedPiece != null) {

                if (!hasKilled) {
                    MovePiece(selectedPiece, x1, y1, x2, y2);
                    EndTurn();

                } else if (hasKilledPiece.x == x2 && hasKilledPiece.y == y2) {
                    MovePiece(selectedPiece, x1, y1, x2, y2);
                    GameObject go = pieces[(x1 + x2) / 2, (y1 + y2) / 2];
                    pieces[(x1 + x2) / 2, (y1 + y2) / 2] = null;
                    RemoveFromBoard(go);
                    EndTurn();
                } else {
                    Debug.Log("Wrong Piece!");
                }
            }

            startDrag = new Vector2(-1, -1);
            selectedPiece = null;
            return;
        }

        //If is an invalid move
        if (selectedPiece != null) {
            MovePiece(selectedPiece, x1, y1, x1, y1);
        } 

        startDrag = new Vector2(-1, -1);
        selectedPiece = null;
        return;
    }

    private void RemoveFromBoard (GameObject go) {
        go.transform.GetChild(0).transform.position = DeadPiecesSpawn.transform.position + new Vector3(Random.Range(-spawnRange, spawnRange), 0f, Random.Range(-spawnRange, spawnRange));
        go.transform.GetChild(0).gameObject.AddComponent<Rigidbody>();
    }

    private bool IsValidMove(int x1, int y1, int x2, int y2, GameObject _selectedPiece) {

        int deltaMoveX = Mathf.Abs(x2 - x1);
        int deltaMoveY = y2 - y1;

        bool isWhite = false;
        bool isBlack = false;
        bool isWhiteKing = false;
        bool isBlackKing = false;


        if (_selectedPiece != null) {
            if (_selectedPiece.name == "White Piece")
                isWhite = true;

            if (_selectedPiece.name == "Black Piece")
                isBlack = true;

            if (_selectedPiece.name == "White Piece King")
                isWhiteKing = true;

            if (_selectedPiece.name == "Black Piece King")
                isBlackKing = true;
        }

        //Debug.Log(deltaMoveX + ":x, y:" + deltaMoveY);

        //if is outside the board
        if (0 > x1 || x1 > 7 || 0 > y1 || y1 > 7 || 0 > x2 || x2 > 7 || 0 > y2 || y2 > 7)
            return false;

        //if slot is occupied
        if (pieces[x2, y2] != null)
            return false;

        //White Pieces
        if (isWhite) {
            if (deltaMoveX == 1) {
                if (deltaMoveY == 1) {
                    return true;
                }
            } else if (deltaMoveX == 2) {
                if (deltaMoveY == 2) {
                    GameObject go = pieces[(x1 + x2) / 2, (y1 + y2) / 2];
                    if (go != null && go.name != "White Piece" && go.name != "White Piece King") {
                        hasKilled = true;
                        hasKilledPiece.x = x2;
                        hasKilledPiece.y = y2;
                        return true;
                    }
                }
            }
        }

        //Black Pieces
        if (isBlack) {
            if (deltaMoveX == 1) {
                if (deltaMoveY == -1) {
                    return true;
                }
            } else if (deltaMoveX == 2) {
                if (deltaMoveY == -2) {
                    GameObject go = pieces[(x1 + x2) / 2, (y1 + y2) / 2];
                    if (go != null && go.name != "Black Piece" && go.name != "Black Piece King") {
                        hasKilled = true;
                        hasKilledPiece.x = x2;
                        hasKilledPiece.y = y2;
                        return true;
                    }
                }
            }
        }

        if (isWhiteKing) {
            if (deltaMoveX == 1) {
                if (deltaMoveY == -1 || deltaMoveY == 1) {
                    return true;
                }
            } else if (deltaMoveX == 2) {
                if (deltaMoveY == -2 || deltaMoveY == 2) {
                    GameObject go = pieces[(x1 + x2) / 2, (y1 + y2) / 2];
                    if (go != null && go.name != "White Piece" && go.name != "White Piece King") {
                        hasKilled = true;
                        hasKilledPiece.x = x2;
                        hasKilledPiece.y = y2;
                        return true;
                    }
                }
            }
        }

        if (isBlackKing) {
            if (deltaMoveX == 1) {
                if (deltaMoveY == -1 || deltaMoveY == 1) {
                    return true;
                }
            } else if (deltaMoveX == 2) {
                if (deltaMoveY == -2 || deltaMoveY == 2) {
                    GameObject go = pieces[(x1 + x2) / 2, (y1 + y2) / 2];
                    if (go != null && go.name != "Black Piece" && go.name != "Black Piece King") {
                        hasKilled = true;
                        hasKilledPiece.x = x2;
                        hasKilledPiece.y = y2;
                        return true;
                    }
                }
            }
        }



        return false;
    }

    private bool IsForceToMove(int x, int y) {
        
        if (pieces[x, y] == null) 
            return false;
        

        //White pieces
        if (pieces[x, y].name == "White Piece" || pieces[x, y].name == "White Piece King") {

            //Top Left
            if (x >= 2 && y <= 5) {

                //if can jump over diffrent color piece
                if (pieces[x - 1, y + 1] != null) {
                    if (pieces[x - 1, y + 1].name == "Black Piece" || pieces[x - 1, y + 1].name == "Black Piece King") {
                        if (pieces[x - 2, y + 2] == null) {
                            return true;
                        }
                    }
                }
            }

            //Top Right
            if (x <= 5 && y <= 5) {

                //if can jump over diffrent color piece
                if (pieces[x + 1, y + 1] != null) {
                    if (pieces[x + 1, y + 1].name == "Black Piece" || pieces[x + 1, y + 1].name == "Black Piece King") {
                        if (pieces[x + 2, y + 2] == null) {
                            return true;
                        }
                    }
                }
            }

        }

        //Black pieces
        if (pieces[x, y].name == "Black Piece" || pieces[x, y].name == "Black Piece King") {

            //Bot Left
            if (x >= 2 && y >= 2) {

                //if can jump over diffrent color piece
                if (pieces[x - 1, y - 1] != null) {
                    if (pieces[x - 1, y - 1].name == "White Piece" || pieces[x - 1, y - 1].name == "White Piece King") {
                        if (pieces[x - 2, y - 2] == null) {
                            return true;
                        }
                    }
                }
            }

            //Bot Right
            if (x <= 5 && y >= 2) {

                //if can jump over diffrent color piece
                if (pieces[x + 1, y - 1] != null) {
                    if (pieces[x + 1, y - 1].name == "White Piece" || pieces[x + 1, y - 1].name == "White Piece King") {
                        if (pieces[x + 2, y - 2] == null) {
                            return true;
                        }
                    }
                }
            }

        }

        //White Pices King
        if (pieces[x, y].name == "White Piece King") {

            //Bot Left
            if (x >= 2 && y >= 2) {

                //if can jump over diffrent color piece
                if (pieces[x - 1, y - 1] != null) {
                    if (pieces[x - 1, y - 1].name == "Black Piece" || pieces[x - 1, y - 1].name == "Black Piece King") {
                        if (pieces[x - 2, y - 2] == null) {
                            return true;
                        }
                    }
                }
            }

            //Bot Right
            if (x <= 5 && y >= 2) {

                //if can jump over diffrent color piece
                if (pieces[x + 1, y - 1] != null) {
                    if (pieces[x + 1, y - 1].name == "Black Piece" || pieces[x + 1, y - 1].name == "Black Piece King") {
                        if (pieces[x + 2, y - 2] == null) {
                            return true;
                        }
                    }
                }
            }

        }

        //Black pieces King
        if (pieces[x, y].name == "Black Piece King") {

            //Top Left
            if (x >= 2 && y <= 5) {

                //if can jump over diffrent color piece
                if (pieces[x - 1, y + 1] != null) {
                    if (pieces[x - 1, y + 1].name == "White Piece" || pieces[x - 1, y + 1].name == "White Piece King") {
                        if (pieces[x - 2, y + 2] == null) {
                            return true;
                        }
                    }
                }
            }

            //Top Right
            if (x <= 5 && y <= 5) {

                //if can jump over diffrent color piece
                if (pieces[x + 1, y + 1] != null) {
                    if (pieces[x + 1, y + 1].name == "White Piece" || pieces[x + 1, y + 1].name == "White Piece King") {
                        if (pieces[x + 2, y + 2] == null) {
                            return true;
                        }
                    }
                }
            }

        }

        return false;
    }

    private List<GameObject> ScanForPossibleForceMove(int x, int y) {

        forcedPieces = new List<GameObject>();

        if (IsForceToMove(x, y)) {
            forcedPieces.Add(pieces[x, y]);
        }

        return forcedPieces;
    }

    private List<GameObject> ScanForPossibleForceMove() {

        forcedPieces = new List<GameObject>();

        for (int i = 0; i < 8; i++) {
            for (int j = 0; j < 8; j++) {
                if (pieces[i, j] != null) {
                    if (isWhiteTurn && pieces[i, j].name == "White Piece" || isWhiteTurn && pieces[i, j].name == "White Piece King" ||
                        !isWhiteTurn && pieces[i, j].name == "Black Piece" || !isWhiteTurn && pieces[i, j].name == "Black Piece King") {
                        if (IsForceToMove(i, j)) {
                            forcedPieces.Add(pieces[i, j]);
                        }
                    }
                }
            }
        }
        return forcedPieces;
    }

    private void EndTurn() {

        int x = (int)hasKilledPiece.x;
        int y = (int)hasKilledPiece.y;
        

        startDrag = new Vector2(-1, -1);
        selectedPiece = null;

        if (ScanForPossibleForceMove(x, y).Count != 0 && hasKilled) {
            moveablePieces = ScanForMoveable();
            HighlightMoveable();
            hasKilledPiece.x = 0;
            hasKilledPiece.y = 0;
            if (isAiPlaying && !isWhiteTurn) 
                AiMakeMove();

            return;
        }
            


        hasKilled = false;
        isWhiteTurn = !isWhiteTurn;

        hasKilledPiece.x = 0;
        hasKilledPiece.y = 0;

        forcedPieces = ScanForPossibleForceMove();
        moveablePieces = ScanForMoveable();
        HighlightMoveable();
        CheckVictory();

        currentPlayerIden.transform.GetComponent<MeshRenderer>().material = (isWhiteTurn) ? WhiteMat : BlackMat;

        if (isAiPlaying && !isWhiteTurn) 
            AiMakeMove();
        
    }

    private void CheckForKing(GameObject p, int y) {
        if (p.name == "White Piece" && y == 7) {
            p.name = "White Piece King";
            p.transform.rotation = new Quaternion(0f, 0f, 180f, 0f);
        }

        if (p.name == "Black Piece" && y == 0) {
            p.name = "Black Piece King";
            p.transform.rotation = new Quaternion(0f, 0f, 180f, 0f);
        }
    }

    //Add game stop if anybody wins
    private void CheckVictory() {
        if (moveablePieces.Count == 0 && isWhiteTurn) {
            Debug.Log("BlackWins!");
            winPlayerIden.transform.GetComponent<MeshRenderer>().material = BlackMat;
        }
        if (moveablePieces.Count == 0 && !isWhiteTurn) {
            Debug.Log("WhiteWins!");
            winPlayerIden.transform.GetComponent<MeshRenderer>().material = WhiteMat;
        }
    }

    private List<GameObject> ScanForMoveable() {

        moveablePieces = new List<GameObject>();

        if (forcedPieces.Count != 0) {
            moveablePieces = forcedPieces;
            return moveablePieces;
        }

        if (isWhiteTurn) {
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    if (pieces[i, j] != null) {
                        if (pieces[i, j].name == "White Piece" || pieces[i, j].name == "White Piece King") {
                            if (IsValidMove(i, j, i + 1, j + 1, pieces[i, j]) || IsValidMove(i, j, i - 1, j + 1, pieces[i, j])) {
                                moveablePieces.Add(pieces[i, j]);
                            }
                        }
                        if (pieces[i, j].name == "White Piece King") {
                            if (IsValidMove(i, j, i + 1, j - 1, pieces[i, j]) || IsValidMove(i, j, i - 1, j - 1, pieces[i, j])) {
                                moveablePieces.Add(pieces[i, j]);
                            }
                        }
                    }
                }
            }
        }

        if (!isWhiteTurn) {
            for (int i = 0; i < 8; i++) {
                for (int j = 0; j < 8; j++) {
                    if (pieces[i, j] != null) {
                        if (pieces[i, j].name == "Black Piece" || pieces[i, j].name == "Black Piece King") {
                            if (IsValidMove(i, j, i + 1, j - 1, pieces[i, j]) || IsValidMove(i, j, i - 1, j - 1, pieces[i, j])) {
                                moveablePieces.Add(pieces[i, j]);
                            }
                        }
                        if (pieces[i, j].name == "Black Piece King") {
                            if (IsValidMove(i, j, i + 1, j + 1, pieces[i, j]) || IsValidMove(i, j, i - 1, j + 1, pieces[i, j])) {
                                moveablePieces.Add(pieces[i, j]);
                            }
                        }
                    }
                }
            }
        }


        return moveablePieces;
    }

    private void HighlightMoveable() {

        for (int i = 0; i < 8; i++) 
            for (int j = 0; j < 8; j++) 
                if (pieces[i, j] != null) {
                    if (pieces[i, j].tag == "White") {
                        pieces[i, j].transform.GetChild(0).transform.GetComponent<MeshRenderer>().material = WhiteMat;
                    } else if (pieces[i, j].tag == "Black") {
                        pieces[i, j].transform.GetChild(0).transform.GetComponent<MeshRenderer>().material = BlackMat;
                    }
                }
                

        foreach (GameObject value in moveablePieces) {
            if (value.tag == "White") {
                value.transform.GetChild(0).transform.GetComponent<MeshRenderer>().material = WhiteActiveMat;
            } else if (value.tag == "Black") {
                value.transform.GetChild(0).transform.GetComponent<MeshRenderer>().material = BlackActiveMat;
            }  
        }
    }




    public void AiMakeMove() {
        int x;
        int y;

        //If there is a forced piece
        if (forcedPieces.Count != 0) {
            Debug.Log("AiMoveForced!");
            GameObject randomForced = forcedPieces[Random.Range(0, forcedPieces.Count)];
            foreach (GameObject item in pieces) {
                if (item == randomForced) {
                    x = (int)(item.transform.position.x + 3.5);
                    y = (int)(item.transform.position.z + 3.5);
                    GameObject go = pieces[x, y];

                    //Add double kill 
                    while (true) {
                        switch (Random.Range(0, 4)) {
                            case 0:
                                if (IsValidMove(x, y, x - 2, y - 2, go)) {
                                    MovePiece(go, x, y, x - 2, y - 2);
                                    GameObject _go = pieces[x - 1, y - 1];
                                    pieces[x - 1, y - 1] = null;
                                    RemoveFromBoard(_go);
                                    hasKilled = true;
                                    EndTurn();
                                    return;
                                }
                                break;
                            case 1:
                                if (IsValidMove(x, y, x + 2, y - 2, go)) {
                                    MovePiece(go, x, y, x + 2, y - 2);
                                    GameObject _go = pieces[x + 1, y - 1];
                                    pieces[x + 1, y - 1] = null;
                                    RemoveFromBoard(_go);
                                    hasKilled = true;
                                    EndTurn();
                                    return;
                                }
                                break;
                            case 2:
                                if (IsValidMove(x, y, x - 2, y + 2, go)) {
                                    MovePiece(go, x, y, x - 2, y + 2);
                                    GameObject _go = pieces[x - 1, y + 1];
                                    pieces[x - 1, y + 1] = null;
                                    RemoveFromBoard(_go);
                                    hasKilled = true;
                                    EndTurn();
                                    return;
                                }
                                break;
                            case 3:
                                if (IsValidMove(x, y, x + 2, y + 2, go)) {
                                    MovePiece(go, x, y, x + 2, y + 2);
                                    GameObject _go = pieces[x + 1, y + 1];
                                    pieces[x + 1, y + 1] = null;
                                    RemoveFromBoard(_go);
                                    hasKilled = true;
                                    EndTurn();
                                    return;
                                }
                                break;
                        }
                    }

                }
            }
        }

        //If there is a safe move for AI
        ScanForAiIsAbleToMakeSafeMove();
        if(AiSafeMovePieces.Count != 0) {
            Debug.Log("AiSafeMove!");
            Debug.Log(AiSafeMovePieces.Count);
            GameObject randomSafeMoved = AiSafeMovePieces[Random.Range(0, AiSafeMovePieces.Count)];
            foreach (GameObject item in pieces) {
                if (item == randomSafeMoved) {
                    x = (int)(item.transform.position.x + 3.5);
                    y = (int)(item.transform.position.z + 3.5);
                    GameObject go = pieces[x, y];

                    //Same, must make it random, not from bot left first
                    while (true) {
                        switch ((pieces[x, y].name == "Black Piece") ? Random.Range(0, 2) : Random.Range(0, 4)) {
                            case 0:
                                if (AiIsAbleToMakeSafeMoveBottomLeft(x, y)) {
                                    MovePiece(go, x, y, x - 1, y - 1);
                                    EndTurn();
                                    return;
                                }
                                break;

                            case 1:
                                if (AiIsAbleToMakeSafeMoveBottomRight(x, y)) {
                                    MovePiece(go, x, y, x + 1, y - 1);
                                    EndTurn();
                                    return;
                                }
                                break;

                            case 2:
                                if (AiIsAbleToMakeSafeMoveTopLeft(x, y)) {
                                    MovePiece(go, x, y, x - 1, y + 1);
                                    EndTurn();
                                    return;
                                }
                                break;

                            case 3:
                                if (AiIsAbleToMakeSafeMoveTopRight(x, y)) {
                                    MovePiece(go, x, y, x + 1, y + 1);
                                    EndTurn();
                                    return;
                                }
                                break;
                        }
                    }


                }
            }
        }

        //If AI has no other move
        Debug.Log("AiMoveNoOtherMoveWithoutDying");
        GameObject randomMoveable = moveablePieces[Random.Range(0, moveablePieces.Count)];
        foreach (GameObject item in pieces) {
            if (item == randomMoveable) {
                x = (int)(item.transform.position.x + 3.5);
                y = (int)(item.transform.position.z + 3.5);
                GameObject go = pieces[x, y];

                //Need Testing!
                while (true) {
                    switch (Random.Range(0, 4)) {
                        case 0:
                            if (IsValidMove(x, y, x - 1, y - 1, go)) {
                                MovePiece(go, x, y, x - 1, y - 1);
                                EndTurn();
                                return;
                            }
                            break;

                        case 1:
                            if (IsValidMove(x, y, x + 1, y - 1, go)) {
                                MovePiece(go, x, y, x + 1, y - 1);
                                EndTurn();
                                return;
                            }
                            break;

                        case 2:
                            if (IsValidMove(x, y, x - 1, y + 1, go)) {
                                MovePiece(go, x, y, x - 1, y + 1);
                                EndTurn();
                                return;
                            }
                            break;

                        case 3:
                            if (IsValidMove(x, y, x + 1, y + 1, go)) {
                                MovePiece(go, x, y, x + 1, y + 1);
                                EndTurn();
                                return;
                            }
                            break;
                    }
                }
            }
        }

    }

    private bool AiIsAbleToMakeSafeMove(int x, int y) {
        //Works only with black, only black is going to be controled by AI

        if (pieces[x, y] == null || pieces[x, y].tag == "White")
            return false;

        //Check if moved, enemy is able to kill other piece
        if (x >= 2 && y >= 2)
            if (pieces[x - 1, y - 1] != null && pieces[x - 2, y - 2] != null)
                if (pieces[x - 1, y - 1].tag == "Black")
                    if (pieces[x - 2, y - 2].tag == "White")
                        return false;

        if (x <= 5 && y >= 2)
            if (pieces[x + 1, y - 1] != null && pieces[x + 2, y - 2] != null)
                if (pieces[x + 1, y - 1].tag == "Black")
                    if (pieces[x + 2, y - 2].tag == "White")
                        return false;

        if (x >= 2 && y <= 5)
            if (pieces[x - 1, y + 1] != null && pieces[x - 2, y + 2] != null)
                if (pieces[x - 1, y + 1].tag == "Black")
                    if (pieces[x - 2, y + 2].tag == "White")
                        return false;

        if (x <= 5 && y <= 5)
            if (pieces[x + 1, y + 1] != null && pieces[x + 2, y + 2] != null)
                if (pieces[x + 1, y + 1].tag == "Black")
                    if (pieces[x + 2, y + 2].tag == "White")
                        return false;



        //Check safesness in potencial new position
        if (pieces[x, y].tag == "Black") {

            if (AiIsAbleToMakeSafeMoveBottomLeft(x, y))
                return true;

            if (AiIsAbleToMakeSafeMoveBottomRight(x, y))
                return true;

        }
        if (pieces[x, y].name == "Black Piece King") {

            if (AiIsAbleToMakeSafeMoveTopLeft(x, y))
                return true;

            if (AiIsAbleToMakeSafeMoveTopRight(x, y))
                return true;

        }

        return false;
    }

    private bool AiIsAbleToMakeSafeMoveBottomLeft(int x, int y) {

        //Bottom Left
        if (x >= 2 && y >= 2)
            if (pieces[x - 1, y - 1] == null) {

                if (pieces[x - 2, y - 2] != null)
                    if (pieces[x - 2, y - 2].tag == "White") 
                        return false;

                if (pieces[x - 2, y] != null)
                    if (pieces[x - 2, y].name == "White Piece King" && pieces[x, y - 2] == null) 
                        return false;
                
                if (pieces[x, y - 2])
                    if (pieces[x - 2, y] == null && pieces[x, y - 2].tag == "White") 
                        return false;

                return true;
                
            } else {
                return false;
            }

        if (x >= 1 && y >= 1)
            if (pieces[x - 1, y - 1] == null)
                return true;

        return false;
    }
    private bool AiIsAbleToMakeSafeMoveBottomRight(int x, int y) {

        //Bottom right
        if (x <= 5 && y >= 2)
            if (pieces[x + 1, y - 1] == null) {

                if (pieces[x + 2, y - 2] != null)
                    if (pieces[x + 2, y - 2].tag == "White") 
                        return false;

                if (pieces[x + 2, y] != null)
                    if (pieces[x + 2, y].name == "White Piece King" && pieces[x, y - 2] == null) 
                        return false;

                if (pieces[x, y - 2] != null)
                    if (pieces[x + 2, y] == null && pieces[x, y - 2].tag == "White") 
                        return false;

                return true;

            } else {
                return false;
            }

        if (x <= 6 && y >= 1)
            if (pieces[x + 1, y - 1] == null)
                return true;

        return false;
    }
    private bool AiIsAbleToMakeSafeMoveTopLeft(int x, int y) {

        //Top Left
        if (x >= 2 && y <= 5)
            if (pieces[x - 1, y + 1] == null) {

                if (pieces[x - 2, y + 2] != null)
                    if (pieces[x - 2, y + 2].name == "White Piece King") 
                        return false;

                if (pieces[x - 2, y] != null)
                    if (pieces[x - 2, y].tag == "White" && pieces[x, y + 2] == null) 
                        return false;

                if (pieces[x, y + 2] != null)
                    if (pieces[x - 2, y] == null && pieces[x, y + 2].name == "White Piece King") 
                        return false;

                return true;

            } else {
                return false;
            }

        if (x >= 1 && y <= 6)
            if (pieces[x - 1, y + 1] == null)
                return true;

        return false;
    }
    private bool AiIsAbleToMakeSafeMoveTopRight(int x, int y) {

        //Top Right
        if (x <= 5 && y <= 5)
            if (pieces[x + 1, y + 1] == null) {

                if (pieces[x + 2, y + 2] != null)
                    if (pieces[x + 2, y + 2].tag == "White Piece King") 
                        return false;

                if (pieces[x + 2, y] != null)
                    if (pieces[x + 2, y].tag == "White" && pieces[x, y + 2] == null) 
                        return false;

                if (pieces[x, y + 2] != null)
                    if (pieces[x + 2, y] == null && pieces[x, y + 2].name == "White Piece King") 
                        return false;

                return true;
                
            } else {
                return false;
            }

        if (x <= 6 && y <= 6)
            if (pieces[x + 1, y + 1] == null)
                return true;

        return false;
    }

    private List<GameObject> ScanForAiIsAbleToMakeSafeMove() {

        AiSafeMovePieces = new List<GameObject>();

        for (int i = 0; i < 8; i++) {
            for (int j = 0; j < 8; j++) {
                if (pieces[i, j] != null) {
                    if (pieces[i, j].tag == "Black") { 
                        if (AiIsAbleToMakeSafeMove(i, j)) {
                            AiSafeMovePieces.Add(pieces[i, j]);
                        }
                    }
                }
            }
        }
        return AiSafeMovePieces;
    }
}
