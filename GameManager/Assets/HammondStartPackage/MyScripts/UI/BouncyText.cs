using UnityEngine;
using TMPro;

public class BouncyText : MonoBehaviour
{
    [Header("Settings")]
    public float bounceStrength = 8f;
    public float springStiffness = 300f;
    public float damping = 12f;
    public float characterSpread = 0.15f;

    private TMP_Text textMesh;
    private float velocityX, velocityY;
    private float displacementX, displacementY;
    private bool isBouncing;

    void Start()
    {
        textMesh = GetComponent<TMP_Text>();
    }

    public void TriggerBounce(float direction)
    {
        velocityY += direction * bounceStrength;
        isBouncing = true;
    }

    public void TriggerBounceSide(float direction)
    {
        velocityX += direction * bounceStrength;
        isBouncing = true;
    }

    void Update()
    {
        if (!isBouncing) return;

        velocityX += (-springStiffness * displacementX - damping * velocityX) * Time.unscaledDeltaTime;
        velocityY += (-springStiffness * displacementY - damping * velocityY) * Time.unscaledDeltaTime;
        displacementX += velocityX * Time.unscaledDeltaTime;
        displacementY += velocityY * Time.unscaledDeltaTime;

        bool settled = Mathf.Abs(velocityX) < 0.01f && Mathf.Abs(displacementX) < 0.01f
                    && Mathf.Abs(velocityY) < 0.01f && Mathf.Abs(displacementY) < 0.01f;

        if (settled)
        {
            isBouncing = false;
            displacementX = displacementY = velocityX = velocityY = 0f;
            textMesh.ForceMeshUpdate();
            for (int i = 0; i < textMesh.textInfo.meshInfo.Length; i++)
            {
                textMesh.textInfo.meshInfo[i].mesh.vertices = textMesh.textInfo.meshInfo[i].vertices;
                textMesh.UpdateGeometry(textMesh.textInfo.meshInfo[i].mesh, i);
            }
            return;
        }

        textMesh.ForceMeshUpdate();
        TMP_TextInfo textInfo = textMesh.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            float ripple = Mathf.Cos(i * characterSpread);
            float xOffset = displacementX * ripple;
            float yOffset = displacementY * ripple;

            vertices[vertexIndex + 0] += new Vector3(xOffset, yOffset, 0);
            vertices[vertexIndex + 1] += new Vector3(xOffset, yOffset, 0);
            vertices[vertexIndex + 2] += new Vector3(xOffset, yOffset, 0);
            vertices[vertexIndex + 3] += new Vector3(xOffset, yOffset, 0);
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textMesh.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}