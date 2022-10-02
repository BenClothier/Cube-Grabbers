namespace Game.Managers
{
    using Game.Utility;

    using System;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class UserInputManager : Singleton<UserInputManager>
    {
        private Controls controls;

        public float PlayerMovementVector { get; private set; }

        public Vector2 MousePos { get; private set; }

        public bool IsJumpPressed { get; private set; }

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

        public event Action<InputAction.CallbackContext> OnJumpPressed
        {
            add
            {
                controls.Default.Jump.performed += value;
            }
            remove
            {
                controls.Default.Jump.performed -= value;
            }
        }

        public event Action<InputAction.CallbackContext> OnJumpReleased
        {
            add
            {
                controls.Default.Jump.canceled += value;
            }
            remove
            {
                controls.Default.Jump.canceled -= value;
            }
        }

        private void Awake()
        {
            controls = new Controls();
        }

        private void OnEnable()
        {
            controls.Default.Move.Enable();
            controls.Default.Jump.Enable();
            controls.Default.MousePos.Enable();
            controls.Default.PrimaryClick.Enable();
            controls.Default.SecondaryClick.Enable();
        }

        private void OnDisable()
        {
            controls.Default.Move.Disable();
            controls.Default.Jump.Disable();
            controls.Default.MousePos.Disable();
            controls.Default.PrimaryClick.Disable();
            controls.Default.SecondaryClick.Disable();
        }

        private void FixedUpdate()
        {
            PlayerMovementVector = controls.Default.Move.ReadValue<float>();
            MousePos = controls.Default.MousePos.ReadValue<Vector2>();
            IsJumpPressed = controls.Default.Jump.IsPressed();
        }
    }
}
