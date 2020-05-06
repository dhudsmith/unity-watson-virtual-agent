using UnityEngine;
using System.Collections;

namespace CrazyMinnow.SALSA.UMA_DCS
{
    public class SalsaUmaSync_ExpressionsTester : MonoBehaviour
    {
        public SalsaUmaSync salsaUmaSync; // Variable reference to the CM_UMAExpressionHelper.cs script
        public float blendSpeed = 3.5f; // How quickly to blend to and from an expression
        public float duration = 2f; // How long to hold the expression once the target amount is reached
        [Range(-1f, 1f)]
        public float rangeOfMotion = 1f; // Target range of motion from -1 to 1
        public SalsaUmaSync.Expression[] shapes; // An enum array to hold multiple expressions

        public bool activateForDuration = false; // Simple inspector button for activating an expression for a duration
        public bool activateToggle = false; // Simple inspector button for activating / deactivating an expression

        private bool isOn = false; // Internally tracks activateToggle status

        void Update()
        {
            // Get reference to the CM_UMAExpressionHelper.cs script
            if (!salsaUmaSync) salsaUmaSync = GetComponent<SalsaUmaSync>();

            // Use the activateForDuration button
            if (activateForDuration)
            {
                activateForDuration = false;

                for (int i = 0; i < shapes.Length; i++)
                {
                    salsaUmaSync.SetExpression(shapes[i], blendSpeed, rangeOfMotion, duration);
                }
            }

            // Use the activateToggle button
            if (activateToggle)
            {
                activateToggle = false;

                if (isOn)
                    isOn = false;
                else
                    isOn = true;

                for (int i = 0; i < shapes.Length; i++)
                {
                    salsaUmaSync.SetExpression(shapes[i], blendSpeed, rangeOfMotion, isOn);
                }
            }
        }
    }
}