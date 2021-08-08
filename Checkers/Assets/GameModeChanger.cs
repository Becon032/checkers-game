using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameModeChanger : MonoBehaviour
{
    Board board = null;

    public Material OnMatAi;
    public Material OffMatAi;

    void Awake() {
        GameObject tempObj = GameObject.Find("Board");
        board = tempObj.GetComponent<Board>();
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            CastRay();
        }
    }

    void CastRay() {
        if ((Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 25f, LayerMask.GetMask("Button")))) {
            if (hit.collider.name == "AiButton") {
                Transform temp = gameObject.transform.Find("AiButton");
                SwitchButton(temp, board.isAiPlaying, OnMatAi, OffMatAi);
                board.isAiPlaying = !board.isAiPlaying;
                if (board.isAiPlaying && !board.isWhiteTurn) {
                    board.AiMakeMove();
                }
            }

            if (hit.collider.name == "RestartGame") {
                SceneManager.LoadScene(0);
            }
        }


    }

    void SwitchButton(Transform tran, bool isPressed, Material onMat, Material offMat) {
        if (isPressed) {
            tran.transform.position += new Vector3(0f, 0.1f, 0f);
            tran.transform.GetComponent<MeshRenderer>().material = offMat;
        } else {
            tran.transform.position -= new Vector3(0f, 0.1f, 0f);
            tran.transform.GetComponent<MeshRenderer>().material = onMat;
        }
    }
}
