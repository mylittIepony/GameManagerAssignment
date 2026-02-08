using UnityEngine;

namespace SymmetryBreakStudio.TastyGrassShader.Example
{
    public class AutoRotate : MonoBehaviour
    {
        public Vector3 rotationAxis;

        public float speed;

        public Vector3 pan;
        public float panSpeed;

        public float fovDelta;

        // Update is called once per frame
        void Update()
        {
            if (TryGetComponent(out Camera cam))
            {
                cam.fieldOfView += fovDelta * Time.deltaTime;
            }

            transform.Translate(pan * (panSpeed * Time.deltaTime));
            transform.Rotate(rotationAxis * (speed * Time.deltaTime));
        }
    }
}