namespace Game.Managers
{
    using Game.Utility;

    using System;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class UserInputManager : Singleton<UserInputManager>
    {
        private Controls controls;

        public Vector2 PlayerMovementVector { get; private set; }

        public Vector2 MousePos { get; private set; }

        public event Action<InputAction.CallbackContext> OnPrimaryMouseDown
        {
            add
            {
                controls.Default.PrimaryClick.performed += value;
            }
            remove
            {
                controls.Default.PrimaryClick.performed -= value;
            }
        }

        public event Action<InputAction.CallbackContext> OnSecondaryMouseDown
        {
            add
            {
                controls.Default.SecondaryClick.performed += value;
            }
            remove
            {
                controls.Default.SecondaryClick.performed -= value;
            }
        }

        public event Action<InputAction.CallbackContext> OnPrimaryMouseUp
        {
            add
            {
                controls.Default.PrimaryClick.canceled += value;
            }
            remove
            {
                controls.Default.PrimaryClick.canceled -= value;
            }
        }

        public event Action<InputAction.CallbackContext> OnSecondaryMouseUp
        {
            add
            {
                controls.Default.SecondaryClick.canceled += value;
            }
            remove
            {
                controls.Default.SecondaryClick.canceled -= value;
            }
        }

        private void Awake()
        {
            controls = new Controls();
        }

        private void OnEnable()
        {
            controls.Default.Move.Enable();
            controls.Default.PrimaryClick.Enable();
            controls.Default.SecondaryClick.Enable();
        }

        private void OnDisable()
        {
            controls.Default.Move.Disable();
            controls.Default.PrimaryClick.Disable();
            controls.Default.SecondaryClick.Disable();
        }

        private void FixedUpdate()
        {
            PlayerMovementVector = controls.Default.Move.ReadValue<Vector2>();
            Debug.Log(controls.Default.MousePos.ReadValue<Vector2>());
            MousePos = controls.Default.MousePos.ReadValue<Vector2>();
        }
    }
}
