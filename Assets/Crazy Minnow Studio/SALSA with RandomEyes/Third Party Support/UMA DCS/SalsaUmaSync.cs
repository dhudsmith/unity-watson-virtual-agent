using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UMA;
using UMA.PoseTools;
using UMA.CharacterSystem;
using CrazyMinnow.SALSA;

namespace CrazyMinnow.SALSA.UMA_DCS
{
    /// <summary>
    /// SalsaUmaSync.cs is designed to work with UMA 2 version 2.5.0 and above,
    /// and uses the new Dynamic Character System (DCS). It provides the linkage
    /// between Salsa3D, RandomEyes3D, UMA 2, and DCS.
    /// </summary>
    [AddComponentMenu("Crazy Minnow Studio/SALSA/Addons/UMA DCS/SalsaUmaSync")]
    public class SalsaUmaSync : MonoBehaviour
    {
        public Salsa3D salsa3D; // Salsa3D component
        public enum EyeControl { UseExpressionPlayer, UseRandomEyes3D } // Enum to select eye control (RandomEyes3D or UMAExpression)
        public EyeControl eyeControl = EyeControl.UseRandomEyes3D;
        public RandomEyes3D randomEyes3D; // RandomEyes component (if selected for eye control)
        public RuntimeAnimatorController animatorController; // RuntimeAnimatorController to be added to the character at runtime
        public UMAExpressionSet expressionSet; // UMAExpression set to be added
        public UMAExpressionPlayer expressionPlayer; // UMAExpressionPlayer to be added
        public enum Expression // UMAExpressionPlayer expressions enum
        {
            jawOpen_Close, jawForward_Back, jawLeft_Right, mouthLeft_Right,
            mouthUp_Down, mouthNarrow_Pucker, tongueOut, tongueCurl, tongueUp_Down, tongueLeft_Right,
            tongueWide_Narrow, leftMouthSmile_Frown, rightMouthSmile_Frown, leftLowerLipUp_Down,
            rightLowerLipUp_Down, leftUpperLipUp_Down, rightUpperLipUp_Down, leftCheekPuff_Squint,
            rightCheekPuff_Squint, noseSneer, browsIn, leftBrowUp_Down, rightBrowUp_Down, midBrowUp_Down
        }
        public List<UmaShape> shapes = new List<UmaShape>(); // Speech shapes
        public List<UmaShapeValueGroup> shapeValueGroups = new List<UmaShapeValueGroup>(); // Speech value groups
        public int expressionCount = 4; // This will be replaced with salsa.expressions.Count in SALSA 2.0
        public bool showSpeechValueGroups; // collapsible inspector section 
        public bool lockShapesOverride = false; // Allows animation when SALSA is not driven by the associated AudioSource

        private DynamicCharacterAvatar dca; // For runtime CharacterCreated listener
        private Transform leftEye; // Used to set the RandomEyes gizmo for eye tracking
        private Transform rightEye; // Used to set the RandomEyes gizmo for eye tracking		
        public bool lockShapes; // Used to allow access to shape group shapes when SALSA is not talking
        private bool characterCreated = false; // Tracks when the CharacterCreated event has been fired
        private float closeEnough = 0.1f; // Used to compare float values
        private bool showWarning = true; // Show config warnings

        /// <summary>
        /// Reset fire the Initialize method
        /// </summary>
        private void Reset()
        {
            Initialize();
        }

        /// <summary>
        /// Add a runtime listener for the UMA CharacterCreated event.
        /// Link or verify links for Salsa3D, RandomEyes3D, set the default shapes and group values.
        /// </summary>
        private void Start()
        {
            showWarning = true;
            dca = GetComponent<DynamicCharacterAvatar>();
            if (dca)
            {
                dca.CharacterCreated.AddListener(new UnityEngine.Events.UnityAction<UMAData>(CharacterCreated));
                if (animatorController)
                    dca.animationController = animatorController;
            }

            FindSalsa3D();
            FindRandomEyes3D();
            DefaultShapes();
            SetValueGroups();
            SetValueGroupsDefaultValues();
        }

        /// <summary>
        /// Process lipsync speech group values and RandomEyes3D eye control (when enabled)
        /// </summary>
        private void LateUpdate()
        {
            if (salsa3D && characterCreated)
            {
                if (salsa3D.isTalking)
                {
                    lockShapes = true;
                }
                else
                {
                    lockShapes = false;
                    for (int i=0; i<shapes.Count; i++)
                    {
                        if (Mathf.Abs(shapes[i].shapeValue) - Mathf.Abs(shapeValueGroups[0].umaShapeAmounts[i].shapePercent) > closeEnough)
                            lockShapes = true;
                    }
                }
            }

            if ((salsa3D && lockShapes) || (salsa3D && lockShapesOverride))
            {
                for (int i = 0; i < shapes.Count; i++)
                {
                    if (salsa3D.sayIndex == 0)
                        shapes[i].shapeValue = Mathf.Lerp(
                            shapes[i].shapeValue, 
                            shapeValueGroups[0].umaShapeAmounts[i].shapePercent, 
                            Time.deltaTime * salsa3D.blendSpeed);
                    else
                        shapes[i].shapeValue = Mathf.Lerp(
                            shapes[i].shapeValue, 
                            shapeValueGroups[salsa3D.sayIndex].umaShapeAmounts[i].shapePercent * (salsa3D.rangeOfMotion / 100), 
                            Time.deltaTime * salsa3D.blendSpeed);

                    SetValue(shapes[i].shape, shapes[i].shapeValue);                    
                }
            }

            /* Here we use the RandomEyes3D.lookAmount values to drive the eye look and blink sliders.
             * You can also set a look target for eye tracking:
             *		randomEyes3D.SetLookTarget(GameObject obj);
             * or enable target affinity. This works like an attention span when tracking a target
             *		randomEyes3D.SetTargetAffinity(bool status);
             * and set the affinity percentage, or the percentage of time to track the target.
             * The remainder of the time will be filled with random looking around.
             *		randomEyes3D.SetAffinityPercentage(float percent);
             */
            if (randomEyes3D && characterCreated && eyeControl == EyeControl.UseRandomEyes3D)
            {
                if (expressionPlayer)
                {
                    // Blink
                    expressionPlayer.leftEyeOpen_Close = randomEyes3D.lookAmount.blink / 100 * -1;
                    expressionPlayer.rightEyeOpen_Close = expressionPlayer.leftEyeOpen_Close;
                    // LookUp
                    if (randomEyes3D.lookAmount.lookUp > 0)
                    {
                        expressionPlayer.leftEyeUp_Down = randomEyes3D.lookAmount.lookUp / 100;
                        expressionPlayer.rightEyeUp_Down = randomEyes3D.lookAmount.lookUp / 100;
                    }
                    // LookDown
                    if (randomEyes3D.lookAmount.lookDown > 0)
                    {
                        expressionPlayer.leftEyeUp_Down = randomEyes3D.lookAmount.lookDown / 100 * -1;
                        expressionPlayer.rightEyeUp_Down = randomEyes3D.lookAmount.lookDown / 100 * -1;
                    }
                    // LookLeft
                    if (randomEyes3D.lookAmount.lookLeft > 0)
                    {
                        expressionPlayer.leftEyeIn_Out = randomEyes3D.lookAmount.lookLeft / 100 * -1;
                        expressionPlayer.rightEyeIn_Out = randomEyes3D.lookAmount.lookLeft / 100;
                    }
                    // LookRight
                    if (randomEyes3D.lookAmount.lookRight > 0)
                    {
                        expressionPlayer.leftEyeIn_Out = randomEyes3D.lookAmount.lookRight / 100;
                        expressionPlayer.rightEyeIn_Out = randomEyes3D.lookAmount.lookRight / 100 * -1;
                    }
                }
            }
        }

        /// <summary>
        /// Get an UMAExpressionPlayer expression value
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private float GetValue(Expression expression)
        {
            float amount = 0f;

            if (expressionPlayer)
            {
                switch (expression)
                {
                    case Expression.jawOpen_Close:
                        amount = expressionPlayer.jawOpen_Close;
                        break;
                    case Expression.jawForward_Back:
                        amount = expressionPlayer.jawForward_Back;
                        break;
                    case Expression.jawLeft_Right:
                        amount = expressionPlayer.jawLeft_Right;
                        break;
                    case Expression.mouthLeft_Right:
                        amount = expressionPlayer.mouthLeft_Right;
                        break;
                    case Expression.mouthUp_Down:
                        amount = expressionPlayer.mouthUp_Down;
                        break;
                    case Expression.mouthNarrow_Pucker:
                        amount = expressionPlayer.mouthNarrow_Pucker;
                        break;
                    case Expression.tongueOut:
                        amount = expressionPlayer.tongueOut;
                        break;
                    case Expression.tongueCurl:
                        amount = expressionPlayer.tongueCurl;
                        break;
                    case Expression.tongueUp_Down:
                        amount = expressionPlayer.tongueUp_Down;
                        break;
                    case Expression.tongueLeft_Right:
                        amount = expressionPlayer.tongueLeft_Right;
                        break;
                    case Expression.tongueWide_Narrow:
                        amount = expressionPlayer.tongueWide_Narrow;
                        break;
                    case Expression.leftMouthSmile_Frown:
                        amount = expressionPlayer.leftMouthSmile_Frown;
                        break;
                    case Expression.rightMouthSmile_Frown:
                        amount = expressionPlayer.rightMouthSmile_Frown;
                        break;
                    case Expression.leftLowerLipUp_Down:
                        amount = expressionPlayer.leftLowerLipUp_Down;
                        break;
                    case Expression.rightLowerLipUp_Down:
                        amount = expressionPlayer.rightLowerLipUp_Down;
                        break;
                    case Expression.leftUpperLipUp_Down:
                        amount = expressionPlayer.leftUpperLipUp_Down;
                        break;
                    case Expression.rightUpperLipUp_Down:
                        amount = expressionPlayer.rightUpperLipUp_Down;
                        break;
                    case Expression.leftCheekPuff_Squint:
                        amount = expressionPlayer.leftCheekPuff_Squint;
                        break;
                    case Expression.rightCheekPuff_Squint:
                        amount = expressionPlayer.rightCheekPuff_Squint;
                        break;
                    case Expression.noseSneer:
                        amount = expressionPlayer.noseSneer;
                        break;
                    case Expression.browsIn:
                        amount = expressionPlayer.browsIn;
                        break;
                    case Expression.leftBrowUp_Down:
                        amount = expressionPlayer.leftBrowUp_Down;
                        break;
                    case Expression.rightBrowUp_Down:
                        amount = expressionPlayer.rightBrowUp_Down;
                        break;
                    case Expression.midBrowUp_Down:
                        amount = expressionPlayer.midBrowUp_Down;
                        break;
                }
            }

            return amount;
        }

        /// <summary>
        /// Set an UMAExpressionPlayer expression value
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="amount"></param>
        private void SetValue(Expression expression, float amount)
        {
            if (expressionPlayer)
            {
                switch (expression)
                {
                    case Expression.jawOpen_Close:
                        expressionPlayer.jawOpen_Close = amount;
                        break;
                    case Expression.jawForward_Back:
                        expressionPlayer.jawForward_Back = amount;
                        break;
                    case Expression.jawLeft_Right:
                        expressionPlayer.jawLeft_Right = amount;
                        break;
                    case Expression.mouthLeft_Right:
                        expressionPlayer.mouthLeft_Right = amount;
                        break;
                    case Expression.mouthUp_Down:
                        expressionPlayer.mouthUp_Down = amount;
                        break;
                    case Expression.mouthNarrow_Pucker:
                        expressionPlayer.mouthNarrow_Pucker = amount;
                        break;
                    case Expression.tongueOut:
                        expressionPlayer.tongueOut = amount;
                        break;
                    case Expression.tongueCurl:
                        expressionPlayer.tongueCurl = amount;
                        break;
                    case Expression.tongueUp_Down:
                        expressionPlayer.tongueUp_Down = amount;
                        break;
                    case Expression.tongueLeft_Right:
                        expressionPlayer.tongueLeft_Right = amount;
                        break;
                    case Expression.tongueWide_Narrow:
                        expressionPlayer.tongueWide_Narrow = amount;
                        break;
                    case Expression.leftMouthSmile_Frown:
                        expressionPlayer.leftMouthSmile_Frown = amount;
                        break;
                    case Expression.rightMouthSmile_Frown:
                        expressionPlayer.rightMouthSmile_Frown = amount;
                        break;
                    case Expression.leftLowerLipUp_Down:
                        expressionPlayer.leftLowerLipUp_Down = amount;
                        break;
                    case Expression.rightLowerLipUp_Down:
                        expressionPlayer.rightLowerLipUp_Down = amount;
                        break;
                    case Expression.leftUpperLipUp_Down:
                        expressionPlayer.leftUpperLipUp_Down = amount;
                        break;
                    case Expression.rightUpperLipUp_Down:
                        expressionPlayer.rightUpperLipUp_Down = amount;
                        break;
                    case Expression.leftCheekPuff_Squint:
                        expressionPlayer.leftCheekPuff_Squint = amount;
                        break;
                    case Expression.rightCheekPuff_Squint:
                        expressionPlayer.rightCheekPuff_Squint = amount;
                        break;
                    case Expression.noseSneer:
                        expressionPlayer.noseSneer = amount;
                        break;
                    case Expression.browsIn:
                        expressionPlayer.browsIn = amount;
                        break;
                    case Expression.leftBrowUp_Down:
                        expressionPlayer.leftBrowUp_Down = amount;
                        break;
                    case Expression.rightBrowUp_Down:
                        expressionPlayer.rightBrowUp_Down = amount;
                        break;
                    case Expression.midBrowUp_Down:
                        expressionPlayer.midBrowUp_Down = amount;
                        break;
                }
            }
        }

        /// <summary>
        /// SetExpression is a public method that allows you to smoothly blend any of the 
        /// supported UMA expressions to the specified amount, at the specified blend speed, 
        /// and for the specified duration.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="blendSpeed"></param>
        /// <param name="amount"></param>
        /// <param name="duration"></param>
        public void SetExpression(Expression expression, float blendSpeed, float rangeOfMotion, float duration)
        {
            StartCoroutine(setExpression(expression, blendSpeed, rangeOfMotion, duration));
        }

        /// <summary>
        /// setExpression is a private IEnumerator method that's called from SetExpression,
        /// and allows you to smoothly blend any of the supported UMA expressions to the 
        /// specified amount, at the specified blend speed, and for the specified duration.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="blendSpeed"></param>
        /// <param name="amount"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        private IEnumerator setExpression(Expression expression, float blendSpeed, float rangeOfMotion, float duration)
        {
            float startAmt = GetValue(expression);
            float currAmt = startAmt;
            bool done;

            done = false;
            while (!done)
            {
                currAmt = Mathf.Lerp(currAmt, rangeOfMotion, Time.deltaTime * blendSpeed);
                SetValue(expression, currAmt);
                if (currAmt.ToString("F2") == rangeOfMotion.ToString("F2"))
                {
                    done = true;
                }
                yield return null;
            }

            yield return new WaitForSeconds(duration);

            done = false;
            while (!done)
            {
                currAmt = Mathf.Lerp(currAmt, startAmt, Time.deltaTime * blendSpeed);
                SetValue(expression, currAmt);
                if (currAmt.ToString("F2") == startAmt.ToString("F2"))
                {
                    done = true;
                }
                yield return null;
            }
        }

        /// <summary>
        /// SetExpression is a public method that allows you to smoothly blend any of the 
        /// supported UMA expressions to the specified amount, at the specified blend speed.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="blendSpeed"></param>
        /// <param name="amount"></param>
        /// <param name="isOn"></param>
        public void SetExpression(Expression expression, float blendSpeed, float percentage, bool active)
        {
            StartCoroutine(setExpression(expression, blendSpeed, percentage, active));
        }

        /// <summary>
        /// setExpression is a private IEnumerator method that's called from SetExpression,
        /// and allows you to smoothly blend any of the supported UMA expressions to the 
        /// specified amount, at the specified blend speed.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="blendSpeed"></param>
        /// <param name="amount"></param>
        /// <param name="isOn"></param>
        /// <returns></returns>
        private IEnumerator setExpression(Expression expression, float blendSpeed, float percentage, bool active)
        {
            float zero = 0f;
            float startAmt = GetValue(expression);
            float currAmt = startAmt;
            bool done;

            if (active)
            {
                done = false;
                while (!done)
                {
                    currAmt = Mathf.Lerp(currAmt, percentage, Time.deltaTime * blendSpeed);
                    SetValue(expression, currAmt);
                    if (currAmt.ToString("F1") == percentage.ToString("F1"))
                    {
                        done = true;
                    }
                    yield return null;
                }
            }

            if (!active)
            {
                done = false;
                while (!done)
                {
                    currAmt = Mathf.Lerp(currAmt, zero, Time.deltaTime * blendSpeed);
                    SetValue(expression, currAmt);
                    if (currAmt.ToString("F1") == zero.ToString("F1"))
                    {
                        done = true;
                    }
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Public method for initilaization
        /// </summary>
        public void Initialize()
        {
            FindSalsa3D();
            FindRandomEyes3D();
            DefaultShapes();
            SetValueGroups();
            SetValueGroupsDefaultValues();
        }

        /// <summary>
        /// When using CM_UmaDcsSync in DesignTime mode, connect the 
        /// UMADynamicAvatar Character Created (UMAData) event to this method.
        /// </summary>
        public void CharacterCreated(UMAData umaData)
        {
            if (umaData.transform.GetInstanceID() == transform.GetInstanceID())
            {
                if (!expressionPlayer)
                {
                    if (animatorController)
                    {
                        expressionSet = umaData.umaRecipe.raceData.expressionSet;
                        expressionPlayer = gameObject.AddComponent<UMAExpressionPlayer>();
                        expressionPlayer.expressionSet = expressionSet;
                        expressionPlayer.umaData = umaData;
                        expressionPlayer.Initialize();
                    }
                    else
                    {
                        if (showWarning)
                        {
                            showWarning = false;
                            Debug.LogWarning("Since no RuntimeAnimatorController was linked to SalsaUmaSync.expressionPlayer, an UMAExpressionPlayer was not created.");
                        }
                    }
                }
                if (!salsa3D) salsa3D = GetComponent<Salsa3D>();
                if (!randomEyes3D) randomEyes3D = GetComponent<RandomEyes3D>();
                if (randomEyes3D)
                {
                    // Get child transforms
                    Transform[] items = randomEyes3D.gameObject.GetComponentsInChildren<Transform>();
                    for (int i = 0; i < items.Length; i++)
                    {
                        if (items[i].name == "LeftEye") leftEye = items[i];
                        if (items[i].name == "RightEye") rightEye = items[i];
                    }

                    // Position the RandomEyes gizmo between the eyes
                    if (leftEye && rightEye)
                    {
                        randomEyes3D.eyePosition.transform.position =
                            ((leftEye.transform.position - rightEye.transform.position) * 0.5f) + rightEye.transform.position;

                        randomEyes3D.eyePosition.transform.parent = leftEye.parent;
                    }
                }
                SetEyeControl(eyeControl);
                characterCreated = true;
            }
        }

        /// <summary>
        /// Set eye control to either RandomEyes3D or UMAExpressionPlayer
        /// </summary>
        /// <param name="eyeControl"></param>
        public void SetEyeControl(EyeControl eyeControl)
        {
            if (eyeControl == EyeControl.UseExpressionPlayer)
            {
                if (expressionPlayer)
                {
                    expressionPlayer.enableBlinking = true;
                    expressionPlayer.enableSaccades = true;
                }
                if (randomEyes3D)
                {
                    randomEyes3D.randomEyes = false;
                    
                    // SALSA 1.4
                    // randomEyes3D.blink = false;
                    
                    // SALAS 1.5
                    randomEyes3D.randomBlink = false;
                }
            }
            else // RandomEyes3D
            {
                if (expressionPlayer)
                {
                    expressionPlayer.enableBlinking = false;
                    expressionPlayer.enableSaccades = false;
                }
                if (randomEyes3D)
                {
                    randomEyes3D.randomEyes = true;
                    
                    // SALSA 1.4
                    //randomEyes3D.blink = true;
                    
                    // SALSA 1.5
                    randomEyes3D.randomBlink = true;
                }
            }
        }

        /// <summary>
        /// Add a speech shape
        /// </summary>
        public void AddShape()
        {
            shapes.Add(new UmaShape());
            for (int i = 0; i < shapeValueGroups.Count; i++)
            {
                shapeValueGroups[i].umaShapeAmounts.Add(new UmaShapeAmount());
            }
        }

        /// <summary>
        /// Reemove a speech shape
        /// </summary>
        /// <param name="index"></param>
        public void RemoveShape(int index)
        {
            shapes.RemoveAt(index);
            for (int grp = 0; grp < shapeValueGroups.Count; grp++)
            {
                shapeValueGroups[grp].umaShapeAmounts.RemoveAt(index);
            }
        }

        /// <summary>
        /// Adds SALSA trigger value groups (SALSA 2.0)
        /// </summary>
        public void AddValueGroup()
        {
            shapeValueGroups.Add(new UmaShapeValueGroup());
            if (shapes.Count > 0)
            {
                for (int i = 0; i < shapes.Count; i++)
                {
                    shapeValueGroups[shapeValueGroups.Count - 1].umaShapeAmounts.Add(new UmaShapeAmount());
                }
            }
        }

        /// <summary>
        /// Remove SALSA trigger value groups (SALSA 2.0)
        /// </summary>
        /// <param name="index"></param>
        public void RemoveValueGroup(int index)
        {
            shapeValueGroups.RemoveAt(index);
        }

        /// <summary>
        /// Attempt to link to the Salsa3D component
        /// </summary>
        public void FindSalsa3D()
        {
            if (!salsa3D) salsa3D = GetComponent<Salsa3D>();
            if (!salsa3D) salsa3D = GetComponentInChildren<Salsa3D>();
        }

        /// <summary>
        /// Attempt to link to the RandomEyes3D component
        /// </summary>
        public void FindRandomEyes3D()
        {
            if (!randomEyes3D) randomEyes3D = GetComponent<RandomEyes3D>();
            if (!randomEyes3D) randomEyes3D = GetComponentInChildren<RandomEyes3D>();
        }

        /// <summary>
        /// Set the default speech groups
        /// </summary>
        public void DefaultShapes()
        {
            if (shapes.Count == 0)
            {
                shapes.Clear();
                shapes.Add(new UmaShape(Expression.jawOpen_Close, 0f));
                shapes.Add(new UmaShape(Expression.mouthNarrow_Pucker, 0f));
                shapes.Add(new UmaShape(Expression.tongueUp_Down, 0f));
                shapes.Add(new UmaShape(Expression.leftMouthSmile_Frown, 0f));
                shapes.Add(new UmaShape(Expression.rightMouthSmile_Frown, 0f));
                shapes.Add(new UmaShape(Expression.leftLowerLipUp_Down, 0f));
                shapes.Add(new UmaShape(Expression.rightLowerLipUp_Down, 0f));
            }
        }

        /// <summary>
        /// Set the default number of SALSA trigger groups
        /// </summary>
        public void SetValueGroups()
        {
            if (salsa3D)
            {
                if (expressionCount != shapeValueGroups.Count)
                {
                    shapeValueGroups.Clear();
                    for (int i = 0; i < expressionCount; i++)
                    {
                        AddValueGroup();
                    }
                }
            }
        }

        /// <summary>
        /// Set the default values for each SALSA trigger group
        /// </summary>
        public void SetValueGroupsDefaultValues()
        {
            if (shapeValueGroups.Count == 4)
            {
                if (shapeValueGroups[0].umaShapeAmounts.Count == 7)
                {
                    shapeValueGroups[0].umaShapeAmounts[0].shapePercent = -1f;
                    shapeValueGroups[0].umaShapeAmounts[1].shapePercent = 0f;
                    shapeValueGroups[0].umaShapeAmounts[2].shapePercent = 0f;
                    shapeValueGroups[0].umaShapeAmounts[3].shapePercent = 0f;
                    shapeValueGroups[0].umaShapeAmounts[4].shapePercent = 0f;
                    shapeValueGroups[0].umaShapeAmounts[5].shapePercent = 0f;
                    shapeValueGroups[0].umaShapeAmounts[6].shapePercent = 0f;

                    shapeValueGroups[1].umaShapeAmounts[0].shapePercent = 0.1f;
                    shapeValueGroups[1].umaShapeAmounts[1].shapePercent = 0.6f;
                    shapeValueGroups[1].umaShapeAmounts[2].shapePercent = 0.5f;
                    shapeValueGroups[1].umaShapeAmounts[3].shapePercent = 0f;
                    shapeValueGroups[1].umaShapeAmounts[4].shapePercent = 0f;
                    shapeValueGroups[1].umaShapeAmounts[5].shapePercent = 0f;
                    shapeValueGroups[1].umaShapeAmounts[6].shapePercent = 0f;

                    shapeValueGroups[2].umaShapeAmounts[0].shapePercent = 0.25f;
                    shapeValueGroups[2].umaShapeAmounts[1].shapePercent = 0f;
                    shapeValueGroups[2].umaShapeAmounts[2].shapePercent = 1f;
                    shapeValueGroups[2].umaShapeAmounts[3].shapePercent = 1f;
                    shapeValueGroups[2].umaShapeAmounts[4].shapePercent = 1f;
                    shapeValueGroups[2].umaShapeAmounts[5].shapePercent = -1f;
                    shapeValueGroups[2].umaShapeAmounts[6].shapePercent = -1f;

                    shapeValueGroups[3].umaShapeAmounts[0].shapePercent = 0.7f;
                    shapeValueGroups[3].umaShapeAmounts[1].shapePercent = 1f;
                    shapeValueGroups[3].umaShapeAmounts[2].shapePercent = -1f;
                    shapeValueGroups[3].umaShapeAmounts[3].shapePercent = 0f;
                    shapeValueGroups[3].umaShapeAmounts[4].shapePercent = 0f;
                    shapeValueGroups[3].umaShapeAmounts[5].shapePercent = 0f;
                    shapeValueGroups[3].umaShapeAmounts[6].shapePercent = 0f;
                }
            }
        }
    }

    [System.Serializable]
    public class UmaShape
    {
        public SalsaUmaSync.Expression shape;
        public float shapeValue = 0f;

        public UmaShape()
        {
            this.shape = SalsaUmaSync.Expression.browsIn;
            this.shapeValue = 0f;
        }

        public UmaShape(SalsaUmaSync.Expression shape, float shapeValue)
        {
            this.shape = shape;
            this.shapeValue = shapeValue;
        }
    }

    [System.Serializable]
    public class UmaShapeAmount
    {
        [Range(-1f, 1f)]
        public float shapePercent = 0f;

        public UmaShapeAmount()
        {
            this.shapePercent = 0f;
        }

        public UmaShapeAmount(float shapePercent)
        {
            this.shapePercent = shapePercent;
        }
    }

    [System.Serializable]
    public class UmaShapeValueGroup
    {
        public List<UmaShapeAmount> umaShapeAmounts = new List<UmaShapeAmount>();
    }
}