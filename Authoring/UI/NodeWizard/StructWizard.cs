#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine.UIElements;
using Unity.Behavior.GraphFramework;

namespace Unity.Behavior
{
    internal class StructWizard : VisualElement
    {
        private const string k_NameField = "EnumNameField";
        private const float k_WizardWidth = 316f;

        private const string k_NameFieldPlaceholderName = "New Struct";
        
        internal WizardStepper Stepper;
        private Modal m_Modal;

        internal delegate void OnStructTypeCreatedCallback(string structClassName);
        internal OnStructTypeCreatedCallback OnStructTypeCreated;
        private TextFieldWithValidation m_NameField;

        public StructWizard(Modal modal, WizardStepper stepper)
        {
            m_Modal = modal;
            styleSheets.Add(ResourceLoadAPI.Load<StyleSheet>("Packages/com.unity.behavior/Authoring/UI/NodeWizard/Assets/EnumWizardStylesheet.uss"));
            var viewTemplate = ResourceLoadAPI.Load<VisualTreeAsset>("Packages/com.unity.behavior/Authoring/UI/NodeWizard/Assets/EnumWizardLayout.uxml");
            viewTemplate.CloneTree(this);

            m_NameField = this.Q<TextFieldWithValidation>(k_NameField);
            this.Q<HelpText>().Text = "Creates an empty struct and the classes required for them to be presented in the UI.";            

            Stepper = stepper;
            Stepper.StepperContainer.style.width = k_WizardWidth;

            UpdateCreateButtonState();
            
            Stepper.ConfirmButton.clicked += OnCreateClicked;           
            
            m_NameField.PlaceholderText = k_NameFieldPlaceholderName;
            m_NameField.OnItemValidation += UpdateCreateButtonState;
            m_NameField.Value = m_NameField.PlaceholderText;
            
            UpdateCreateButtonState();
        }

        private void UpdateCreateButtonState()
        {
            Stepper.ConfirmButton.SetEnabled(true);
        }

        private void OnCreateClicked()
        {
            string className = GeneratorUtils.RemoveSpaces(m_NameField.Value);
            if (StructGeneratorUtility.CreateStructAsset(className))
            {
                OnStructTypeCreated?.Invoke(className);
                m_Modal.Dismiss();
            }
        }

        internal void OnShow()
        {
            schedule.Execute(m_NameField.Focus);
        }
    }
}
#endif