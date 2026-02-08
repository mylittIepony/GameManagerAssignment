using UnityEngine;

namespace SymmetryBreakStudio.TastyGrassShader.Example
{
    public class FlyCamera : MonoBehaviour
    {
        public float speed;

        public float smoothing;

        public float mouseSpeed, mouseSmoothing;

        Vector2 _mouse, _mouseVelocity;


        Vector3 _movement, _movementVelocity;
        Quaternion _rotation;

        void Start()
        {
            _rotation = transform.localRotation;
        }

        void Update()
        {
            {
                Vector3 currentMovement = Vector3.zero;


                if (Input.GetKey(KeyCode.W))
                {
                    currentMovement += Vector3.forward;
                }

                if (Input.GetKey(KeyCode.S))
                {
                    currentMovement += Vector3.back;
                }

                if (Input.GetKey(KeyCode.A))
                {
                    currentMovement -= Vector3.right;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    currentMovement += Vector3.right;
                }

                if (Input.GetKey(KeyCode.Q))
                {
                    currentMovement -= Vector3.up;
                }

                if (Input.GetKey(KeyCode.E))
                {
                    currentMovement += Vector3.up;
                }

                currentMovement = currentMovement.normalized;

                _movement = Vector3.SmoothDamp(_movement, currentMovement, ref _movementVelocity, smoothing);

                transform.Translate(_movement * (speed * Time.deltaTime), Space.Self);
            }
            {
                float mouseX = 0.0f, mouseY = 0.0f;
                if (Input.GetMouseButton(1))
                {
                    mouseX = Input.GetAxis("Mouse X");
                    mouseY = Input.GetAxis("Mouse Y");
                }


                _mouse = Vector2.SmoothDamp(_mouse, new Vector2(mouseX, mouseY), ref _mouseVelocity,
                    mouseSmoothing);


                Vector3 currentEulerAngles = _rotation.eulerAngles;
                currentEulerAngles += new Vector3(-_mouse.y, _mouse.x, 0.0f) * (mouseSpeed * Time.deltaTime);

                if (currentEulerAngles.x is <= 90.0f and >= 0.0f)
                {
                    currentEulerAngles.x = Mathf.Clamp(currentEulerAngles.x, 0.0f, 90.0f);
                }

                if (currentEulerAngles.x >= 270.0f)
                {
                    currentEulerAngles.x = Mathf.Clamp(currentEulerAngles.x, 270.0f, 360.0f);
                }

                _rotation.eulerAngles = currentEulerAngles;


                Quaternion newRotation = _rotation;
                Vector3 newRationEuler = newRotation.eulerAngles;
                newRationEuler.z = 0.0f; // never dutch angle
                transform.localRotation = Quaternion.Euler(newRationEuler);
            }
        }
    }
}