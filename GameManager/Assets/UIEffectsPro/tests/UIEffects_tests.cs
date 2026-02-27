using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using System.Collections;

namespace UIEffectsPro.Tests
{
    /// <summary>
    /// Contains EditMode tests for the UIEffectsPro package using the Unity Test Runner.
    /// These tests focus on verifying the core functionality, integration, and proper material
    /// application of UIEffects components without incurring performance overhead.
    /// </summary>
    public class UIEffects_Tests
    {
        // Fields to hold objects required for tests, created and destroyed for each test run.
        private GameObject _testGameObject;
        private Image _testImage;
        private UIEffectsPro.Runtime.UIEffectComponent _effectComponent;
        private UIEffectsPro.Runtime.UIEffectProfile _testProfile;

        /// <summary>
        /// This method is executed before each test. It sets up a consistent
        /// environment for testing by creating necessary GameObjects and components.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            // Create a new GameObject to host the UI components for testing.
            _testGameObject = new GameObject("TestUIEffect");
            // Add an Image component, as UIEffects typically work on UI graphics.
            _testImage = _testGameObject.AddComponent<Image>();

            // Create an instance of a UIEffectProfile for testing.
            // This profile will hold the effect settings.
            _testProfile = ScriptableObject.CreateInstance<UIEffectsPro.Runtime.UIEffectProfile>();
            _testProfile.name = "TestProfile";
            // Assign recognizable, non-default values to the profile for easy assertion.
            _testProfile.globalCornerRadius = 25f;
            _testProfile.borderWidth = 5f;
            _testProfile.borderColor = Color.red;
            _testProfile.fillColor = Color.blue;
            _testProfile.useIndividualCorners = false;
        }

        /// <summary>
        /// This method is executed after each test. It cleans up any objects
        /// created during the setup phase to prevent tests from interfering with each other.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            // Destroy the test GameObject if it exists to clean up the scene.
            if (_testGameObject != null)
            {
                Object.DestroyImmediate(_testGameObject);
            }

            // Destroy the test UIEffectProfile if it exists.
            if (_testProfile != null)
            {
                Object.DestroyImmediate(_testProfile);
            }
        }

        /// <summary>
        /// Verifies that the UIEffectComponent can be successfully added to a GameObject.
        /// </summary>
        [Test]
        public void UIEffectComponent_CanBeAddedToGameObject()
        {
            // Arrange & Act: Add the effect component to our test GameObject.
            _effectComponent = _testGameObject.AddComponent<UIEffectsPro.Runtime.UIEffectComponent>();

            // Assert: Check that the component was actually added and is associated with the correct GameObject.
            Assert.IsNotNull(_effectComponent, "Component should not be null after being added.");
            Assert.AreEqual(_testGameObject, _effectComponent.gameObject, "Component should be attached to the test GameObject.");
        }

        /// <summary>
        /// Ensures that a newly created UIEffectProfile has the correct, expected default values.
        /// </summary>
        [Test]
        public void UIEffectProfile_HasCorrectDefaultValues()
        {
            // Arrange: Create a new, clean instance of a UIEffectProfile.
            var profile = ScriptableObject.CreateInstance<UIEffectsPro.Runtime.UIEffectProfile>();

            // Act: Explicitly call the method that resets the profile to its default state.
            profile.ResetToDefaults();

            // Assert: Verify that each relevant property matches its expected default value.
            Assert.AreEqual(10f, profile.globalCornerRadius, "Default global corner radius is incorrect.");
            Assert.AreEqual(2f, profile.borderWidth, "Default border width is incorrect.");
            Assert.AreEqual(Color.black, profile.borderColor, "Default border color is incorrect.");
            Assert.AreEqual(Color.white, profile.fillColor, "Default fill color is incorrect.");
            Assert.IsFalse(profile.useIndividualCorners, "useIndividualCorners should be false by default.");
            Assert.IsFalse(profile.enableBlur, "Blur should be disabled by default.");
            Assert.IsFalse(profile.enableShadow, "Shadow should be disabled by default.");

            // Cleanup: Destroy the profile instance created for this test.
            Object.DestroyImmediate(profile);
        }

        /// <summary>
        /// Tests that the GetCornerRadii method returns a Vector4 with the correct values
        /// when individual corner settings are enabled.
        /// </summary>
        [Test]
        public void UIEffectProfile_GetCornerRadii_ReturnsCorrectVector4()
        {
            // Arrange: Configure the profile to use individual corners and set distinct values for each.
            _testProfile.useIndividualCorners = true;
            _testProfile.cornerRadiusTopLeft = 10f;
            _testProfile.cornerRadiusTopRight = 20f;
            _testProfile.cornerRadiusBottomLeft = 30f;
            _testProfile.cornerRadiusBottomRight = 40f;

            // Act: Call the method under test.
            Vector4 result = _testProfile.GetCornerRadii();

            // Assert: The returned Vector4 should map the corner values to its components in the correct order (x=TL, y=TR, z=BR, w=BL).
            Assert.AreEqual(10f, result.x, "Top Left corner radius (x) is incorrect.");
            Assert.AreEqual(20f, result.y, "Top Right corner radius (y) is incorrect.");
            Assert.AreEqual(40f, result.z, "Bottom Right corner radius (z) is incorrect.");
            Assert.AreEqual(30f, result.w, "Bottom Left corner radius (w) is incorrect.");
        }

        /// <summary>
        /// Verifies that GetCornerRadii returns a Vector4 with all components equal to the
        /// global corner radius when individual corners are disabled.
        /// </summary>
        [Test]
        public void UIEffectProfile_GetCornerRadii_UsesGlobalWhenIndividualDisabled()
        {
            // Arrange: Ensure individual corners are disabled and set a global radius value.
            _testProfile.useIndividualCorners = false;
            _testProfile.globalCornerRadius = 15f;

            // Act: Call the method under test.
            Vector4 result = _testProfile.GetCornerRadii();

            // Assert: All components of the resulting Vector4 should be equal to the global radius.
            Assert.AreEqual(15f, result.x, "Vector.x should match global radius.");
            Assert.AreEqual(15f, result.y, "Vector.y should match global radius.");
            Assert.AreEqual(15f, result.z, "Vector.z should match global radius.");
            Assert.AreEqual(15f, result.w, "Vector.w should match global radius.");
        }

        /// <summary>
        /// Checks if a UIEffectProfile can be successfully assigned to a UIEffectComponent.
        /// </summary>
        [Test]
        public void UIEffectComponent_CanSetProfile()
        {
            // Arrange: Add the effect component to the test GameObject.
            _effectComponent = _testGameObject.AddComponent<UIEffectsPro.Runtime.UIEffectComponent>();

            // Act: Assign the test profile to the component.
            _effectComponent.SetProfile(_testProfile);

            // Assert: The component's profile property should now be the profile we assigned.
            Assert.AreEqual(_testProfile, _effectComponent.profile, "The profile was not set correctly on the component.");
        }

        /// <summary>
        /// Ensures that the UIEffectComponent does not throw exceptions when its profile is null
        /// and its update methods are called. This is important for stability.
        /// </summary>
        [Test]
        public void UIEffectComponent_HandlesNullProfile()
        {
            // Arrange: Add the component and explicitly set its profile to null.
            _effectComponent = _testGameObject.AddComponent<UIEffectsPro.Runtime.UIEffectComponent>();
            _effectComponent.profile = null;

            // Act & Assert: Call key methods on the component. The test passes if no exceptions are thrown.
            Assert.DoesNotThrow(() =>
            {
                _effectComponent.ApplyProfile();
                _effectComponent.UpdateEffect();
                _effectComponent.ForceUpdate();
            }, "Component methods should not throw exceptions when the profile is null.");
        }

        /// <summary>
        /// Verifies that the Clone method on a UIEffectProfile creates a new, independent instance
        /// with the same values, not just a reference to the original.
        /// </summary>
        [Test]
        public void UIEffectProfile_CloneCreatesIndependentCopy()
        {
            // Arrange: Set some specific values on the original profile.
            _testProfile.globalCornerRadius = 50f;
            _testProfile.borderColor = Color.green;

            // Act: Create a clone of the test profile.
            var clonedProfile = _testProfile.Clone() as UIEffectsPro.Runtime.UIEffectProfile;

            // Assert: Initial checks to ensure the clone is a separate object with identical values.
            Assert.IsNotNull(clonedProfile, "Cloned profile should not be null.");
            Assert.AreNotSame(_testProfile, clonedProfile, "Cloned profile should be a different instance from the original.");
            Assert.AreEqual(_testProfile.globalCornerRadius, clonedProfile.globalCornerRadius, "Cloned corner radius should match original.");
            Assert.AreEqual(_testProfile.borderColor, clonedProfile.borderColor, "Cloned border color should match original.");

            // Act & Assert (part 2): Modify the clone and verify the original remains unchanged.
            clonedProfile.globalCornerRadius = 100f;
            Assert.AreNotEqual(_testProfile.globalCornerRadius, clonedProfile.globalCornerRadius, "Changing the clone's value should not affect the original.");

            // Cleanup: Destroy the cloned profile object.
            Object.DestroyImmediate(clonedProfile);
        }

        /// <summary>
        /// Confirms that the UIEffectComponent works correctly when attached to a GameObject
        /// that has a RawImage component instead of a regular Image component.
        /// </summary>
        [Test]
        public void UIEffectComponent_WorksWithRawImage()
        {
            // Arrange: Create a separate GameObject with a RawImage and the effect component.
            var rawImageObject = new GameObject("TestRawImage");
            rawImageObject.AddComponent<RawImage>();
            var effectComponent = rawImageObject.AddComponent<UIEffectsPro.Runtime.UIEffectComponent>();
            effectComponent.profile = _testProfile;

            // Act & Assert: The test checks that applying the profile doesn't cause an error.
            Assert.DoesNotThrow(() =>
            {
                effectComponent.ApplyProfile();
            }, "ApplyProfile should work with a RawImage without throwing an exception.");

            // Assert: Further verification that the component and profile were set up correctly.
            Assert.IsNotNull(effectComponent);
            Assert.AreEqual(_testProfile, effectComponent.profile);

            // Cleanup: Destroy the temporary GameObject used in this test.
            Object.DestroyImmediate(rawImageObject);
        }

        /// <summary>
        /// A more specific test to ensure the ResetToDefaults method correctly overwrites
        /// existing non-default values with the standard defaults.
        /// </summary>
        [Test]
        public void UIEffectProfile_ResetToDefaults_SetsCorrectValues()
        {
            // Arrange: Create a profile and set its properties to arbitrary, non-default values.
            var profile = ScriptableObject.CreateInstance<UIEffectsPro.Runtime.UIEffectProfile>();
            profile.globalCornerRadius = 999f;
            profile.borderWidth = 999f;
            profile.borderColor = Color.magenta;

            // Act: Call the reset method.
            profile.ResetToDefaults();

            // Assert: Verify that the properties have been changed back to their expected default values.
            Assert.AreEqual(10f, profile.globalCornerRadius, "globalCornerRadius was not reset correctly.");
            Assert.AreEqual(2f, profile.borderWidth, "borderWidth was not reset correctly.");
            Assert.AreEqual(Color.black, profile.borderColor, "borderColor was not reset correctly.");
            Assert.AreEqual(Color.white, profile.fillColor, "fillColor was not reset correctly.");

            // Cleanup: Destroy the test profile.
            Object.DestroyImmediate(profile);
        }

        /// <summary>
        /// Verifies that the BlurParams nested class within the profile is initialized with correct default values.
        /// </summary>
        [Test]
        public void UIEffectProfile_BlurParams_HasCorrectDefaults()
        {
            // Arrange & Act: Create a new profile instance. The BlurParams should be initialized in its constructor.
            var profile = ScriptableObject.CreateInstance<UIEffectsPro.Runtime.UIEffectProfile>();

            // Assert: Check that the BlurParams object exists and its properties have the expected defaults.
            Assert.IsNotNull(profile.blurParams, "BlurParams should not be null.");
            Assert.AreEqual(2, profile.blurParams.downsample, "Default blur downsample is incorrect.");
            Assert.AreEqual(2, profile.blurParams.iterations, "Default blur iterations is incorrect.");
            Assert.AreEqual(2f, profile.blurParams.radius, "Default blur radius is incorrect.");

            // Cleanup
            Object.DestroyImmediate(profile);
        }

        /// <summary>
        /// Verifies that the ShadowParams nested class within the profile is initialized with correct default values.
        /// </summary>
        [Test]
        public void UIEffectProfile_ShadowParams_HasCorrectDefaults()
        {
            // Arrange & Act: Create a new profile instance.
            var profile = ScriptableObject.CreateInstance<UIEffectsPro.Runtime.UIEffectProfile>();

            // Assert: Check that the ShadowParams object exists and its properties have the expected defaults.
            Assert.IsNotNull(profile.shadowParams, "ShadowParams should not be null.");
            Assert.AreEqual(new Color(0f, 0f, 0f, 0.5f), profile.shadowParams.color, "Default shadow color is incorrect.");
            Assert.AreEqual(new Vector2(2f, -2f), profile.shadowParams.offset, "Default shadow offset is incorrect.");
            Assert.AreEqual(3f, profile.shadowParams.blur, "Default shadow blur is incorrect.");
            Assert.AreEqual(0.5f, profile.shadowParams.opacity, "Default shadow opacity is incorrect.");

            // Cleanup
            Object.DestroyImmediate(profile);
        }
        
        /// <summary>
        /// Verifies that the GradientParams nested class within the profile is initialized with correct default values.
        /// </summary>
        [Test]
        public void UIEffectProfile_GradientParams_HasCorrectDefaults()
        {
            // Arrange & Act: Create a new profile instance.
            var profile = ScriptableObject.CreateInstance<UIEffectsPro.Runtime.UIEffectProfile>();
            
            // Assert: Check that the GradientParams object exists and its properties have the expected defaults.
            Assert.IsNotNull(profile.gradientParams, "GradientParams should not be null.");
            Assert.AreEqual(UIEffectsPro.Runtime.UIEffectProfile.GradientParams.GradientType.Linear, profile.gradientParams.type, "Default gradient type should be Linear.");
            Assert.AreEqual(Color.white, profile.gradientParams.colorA, "Default gradient colorA is incorrect.");
            Assert.AreEqual(Color.gray, profile.gradientParams.colorB, "Default gradient colorB is incorrect.");
            Assert.AreEqual(0f, profile.gradientParams.angle, "Default gradient angle is incorrect.");
            Assert.IsFalse(profile.gradientParams.enabled, "Gradient should be disabled by default.");
            
            // Cleanup
            Object.DestroyImmediate(profile);
        }

        /// <summary>
        /// Confirms that the 'autoUpdate' property on the UIEffectComponent defaults to true.
        /// </summary>
        [Test]
        public void UIEffectComponent_AutoUpdate_DefaultsToTrue()
        {
            // Arrange & Act: Add a new component.
            _effectComponent = _testGameObject.AddComponent<UIEffectsPro.Runtime.UIEffectComponent>();

            // Assert: Check the default value of the autoUpdate property.
            Assert.IsTrue(_effectComponent.autoUpdate, "autoUpdate property should be true by default.");
        }

        /// <summary>
        /// A UnityTest (coroutine test) that verifies the component behaves as expected
        /// over a frame in Play Mode, simulating runtime behavior.
        /// </summary>
        [UnityTest]
        public IEnumerator UIEffectComponent_UpdatesInPlayMode()
        {
            // Arrange: Add and configure the component.
            _effectComponent = _testGameObject.AddComponent<UIEffectsPro.Runtime.UIEffectComponent>();
            _effectComponent.profile = _testProfile;
            _effectComponent.autoUpdate = true;

            // Act: Set the profile, which should trigger an update.
            _effectComponent.SetProfile(_testProfile);

            // Wait for one frame to allow Unity's lifecycle methods (like Update) to run.
            yield return null;

            // Assert: After a frame, confirm the component is still enabled and has the correct profile.
            Assert.AreEqual(_testProfile, _effectComponent.profile, "Profile should remain set after one frame.");
            Assert.IsTrue(_effectComponent.enabled, "Component should be enabled in Play Mode.");
        }
        
        /// <summary>
        /// Verifies that BlurParams has the new blurType field with the correct default value.
        /// </summary>
        [Test]
        public void UIEffectProfile_BlurParams_HasBlurTypeProperty()
        {
            // Arrange & Act: Create a new profile instance. The BlurParams should be initialized in its constructor.
            var profile = ScriptableObject.CreateInstance<UIEffectsPro.Runtime.UIEffectProfile>();
            
            // Assert: Check that the BlurParams object exists and its properties have the expected defaults.
            Assert.IsNotNull(profile.blurParams, "BlurParams should not be null.");
            Assert.AreEqual(
                UIEffectsPro.Runtime.UIEffectProfile.BlurParams.BlurType.Internal, 
                profile.blurParams.blurType, 
                "Default blur type should be Internal."
            );
            
            // Cleanup
            Object.DestroyImmediate(profile);
        }
        
        /// <summary>
        /// Verifies that the blur type is correctly cloned.
        /// </summary>
        [Test]
        public void UIEffectProfile_ClonePreservesBlurType()
        {
            // Arrange: Set the blur type to Background and enable blur.
            _testProfile.enableBlur = true;
            _testProfile.blurParams.blurType = UIEffectsPro.Runtime.UIEffectProfile.BlurParams.BlurType.Background;
            
            // Act: Clone the profile.
            var clonedProfile = _testProfile.Clone() as UIEffectsPro.Runtime.UIEffectProfile;
            
            // Assert: The clone's blur type should match the original's.
            Assert.AreEqual(
                UIEffectsPro.Runtime.UIEffectProfile.BlurParams.BlurType.Background,
                clonedProfile.blurParams.blurType,
                "Cloned profile should preserve blur type."
            );
            
            // Cleanup
            Object.DestroyImmediate(clonedProfile);
        }
        
        /// <summary>
        /// Verifies that progress border color gradient properties work correctly.
        /// </summary>
        [Test]
        public void UIEffectProfile_ProgressColorGradient_WorksCorrectly()
        {
            // Arrange
            var profile = ScriptableObject.CreateInstance<UIEffectsPro.Runtime.UIEffectProfile>();
            profile.useProgressBorder = true;
            profile.useProgressColorGradient = true;
            profile.progressColorStart = Color.black;
            profile.progressColorEnd = Color.green;
            profile.progressValue = 0.5f;
                
            // Act
            var clonedProfile = profile.Clone() as UIEffectsPro.Runtime.UIEffectProfile;
                
            // Assert
            Assert.IsTrue(clonedProfile.useProgressColorGradient, "Color gradient should be enabled");
            Assert.AreEqual(Color.black, clonedProfile.progressColorStart, "Start color should be black");
            Assert.AreEqual(Color.green, clonedProfile.progressColorEnd, "End color should be green");
            Assert.AreEqual(0.5f, clonedProfile.progressValue, "Progress value should be 0.5");
                
            // Cleanup
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(clonedProfile);
        }
        /// <summary>
        /// Verifies that border gradient properties have correct default values.
        /// </summary>
        [Test]
        public void UIEffectProfile_BorderGradient_HasCorrectDefaults()
        {
            var profile = ScriptableObject.CreateInstance<UIEffectsPro.Runtime.UIEffectProfile>();
            profile.ResetToDefaults();

            Assert.IsFalse(profile.useBorderGradient, "Border gradient should be disabled by default.");
            Assert.AreEqual(Color.white, profile.borderColorB, "Default borderColorB should be white.");
            Assert.AreEqual(0f, profile.borderGradientAngle, "Default border gradient angle should be 0.");

            Object.DestroyImmediate(profile);
        }

        /// <summary>
        /// Verifies that border gradient properties are correctly cloned.
        /// </summary>
        [Test]
        public void UIEffectProfile_ClonePreservesBorderGradient()
        {
            _testProfile.useBorderGradient = true;
            _testProfile.borderColorB = Color.cyan;
            _testProfile.borderGradientAngle = 45f;

            var clonedProfile = _testProfile.Clone() as UIEffectsPro.Runtime.UIEffectProfile;

            Assert.IsTrue(clonedProfile.useBorderGradient, "Cloned profile should preserve useBorderGradient.");
            Assert.AreEqual(Color.cyan, clonedProfile.borderColorB, "Cloned profile should preserve borderColorB.");
            Assert.AreEqual(45f, clonedProfile.borderGradientAngle, "Cloned profile should preserve borderGradientAngle.");

            Object.DestroyImmediate(clonedProfile);
        }

        /// <summary>
        /// Verifies that border color alpha is properly stored in the profile (opacity test).
        /// </summary>
        [Test]
        public void UIEffectProfile_BorderColorAlpha_IsPreserved()
        {
            _testProfile.borderColor = new Color(1f, 0f, 0f, 0.5f);
            
            var clonedProfile = _testProfile.Clone() as UIEffectsPro.Runtime.UIEffectProfile;
            
            Assert.AreEqual(0.5f, clonedProfile.borderColor.a, 0.001f, "Border color alpha should be preserved in clone.");

            Object.DestroyImmediate(clonedProfile);
        }
    }
}