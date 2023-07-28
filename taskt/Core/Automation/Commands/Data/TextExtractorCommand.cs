﻿using System;
using System.Linq;
using System.Xml.Serialization;
using System.Data;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Windows.Forms;
using taskt.UI.Forms;
using taskt.UI.CustomControls;
using System.Drawing;

namespace taskt.Core.Automation.Commands
{
    [Serializable]
    [Attributes.ClassAttributes.Group("Data Commands")]
    [Attributes.ClassAttributes.SubGruop("Text")]
    [Attributes.ClassAttributes.Description("This command allows you to perform advanced string extraction.")]
    [Attributes.ClassAttributes.UsesDescription("Use this command when you want to extract a piece of text from a larger text or variable")]
    [Attributes.ClassAttributes.ImplementationDescription("This command implements actions against VariableList from the scripting engine.")]
    public class TextExtractorCommand : ScriptCommand
    {
        [XmlAttribute]
        [Attributes.PropertyAttributes.PropertyDescription("Supply the value or variable requiring extraction")]
        [Attributes.PropertyAttributes.PropertyUIHelper(Attributes.PropertyAttributes.PropertyUIHelper.UIAdditionalHelperType.ShowVariableHelper)]
        [Attributes.PropertyAttributes.InputSpecification("Select or provide a variable or text value")]
        [Attributes.PropertyAttributes.SampleUsage("**Hello** or **{{{vText}}}**")]
        [Attributes.PropertyAttributes.Remarks("")]
        [Attributes.PropertyAttributes.PropertyShowSampleUsageInDescription(true)]
        public string v_InputValue { get; set; }

        [XmlAttribute]
        [Attributes.PropertyAttributes.PropertyDescription("Please select text extraction type")]
        [Attributes.PropertyAttributes.InputSpecification("Select the type of extraction that is required.")]
        [Attributes.PropertyAttributes.SampleUsage("Select from Before Text, After Text, Between Text")]
        [Attributes.PropertyAttributes.Remarks("")]
        [Attributes.PropertyAttributes.PropertyUISelectionOption("Extract All After Text")]
        [Attributes.PropertyAttributes.PropertyUISelectionOption("Extract All Before Text")]
        [Attributes.PropertyAttributes.PropertyUISelectionOption("Extract All Between Text")]
        [Attributes.PropertyAttributes.PropertyRecommendedUIControl(Attributes.PropertyAttributes.PropertyRecommendedUIControl.RecommendeUIControlType.ComboBox)]
        public string v_TextExtractionType { get; set; }

        [XmlElement]
        [Attributes.PropertyAttributes.PropertyDescription("Extraction Parameters")]
        [Attributes.PropertyAttributes.PropertyUIHelper(Attributes.PropertyAttributes.PropertyUIHelper.UIAdditionalHelperType.ShowVariableHelper)]
        [Attributes.PropertyAttributes.InputSpecification("Define the required extraction parameters, which is dependent on the type of extraction.")]
        [Attributes.PropertyAttributes.SampleUsage("n/a")]
        [Attributes.PropertyAttributes.Remarks("")]
        [Attributes.PropertyAttributes.PropertyRecommendedUIControl(Attributes.PropertyAttributes.PropertyRecommendedUIControl.RecommendeUIControlType.DataGridView)]
        [Attributes.PropertyAttributes.PropertyDataGridViewSetting(false, false, true, 400, 135)]
        [Attributes.PropertyAttributes.PropertySecondaryLabel(true)]
        [Attributes.PropertyAttributes.PropertyAddtionalParameterInfo("Extract All After Text\tLeading Text", "The beginning of the text to be extracted", "**Hello** or **{{{vStart}}}**")]
        [Attributes.PropertyAttributes.PropertyAddtionalParameterInfo("Extract All After Text\tSkip Past Occurences", "", "")]
        [Attributes.PropertyAttributes.PropertyAddtionalParameterInfo("Extract All Before Text\tTrailing Text", "The end of text to be extracted", "**Hello** or **{{{vEnd}}}**")]
        [Attributes.PropertyAttributes.PropertyAddtionalParameterInfo("Extract All Before Text\tSkip Past Occurences", "")]
        [Attributes.PropertyAttributes.PropertyAddtionalParameterInfo("Extract All Between Text\tLeading Text", "The beginning of the text to be extracted", "**Hello** or **{{{vStart}}}**")]
        [Attributes.PropertyAttributes.PropertyAddtionalParameterInfo("Extract All Between Text\tTrailing Text", "The end of text to be extracted", "**Hello** or **{{{vEnd}}}**")]
        [Attributes.PropertyAttributes.PropertyAddtionalParameterInfo("Extract All Between Text\tSkip Past Occurences", "")]
        public DataTable v_TextExtractionTable { get; set; }

        [XmlAttribute]
        [Attributes.PropertyAttributes.PropertyDescription("Please select the variable to receive the extracted text")]
        [Attributes.PropertyAttributes.InputSpecification("Select or provide a variable from the variable list")]
        [Attributes.PropertyAttributes.SampleUsage("**vSomeVariable**")]
        [Attributes.PropertyAttributes.Remarks("If you have enabled the setting **Create Missing Variables at Runtime** then you are not required to pre-define your variables, however, it is highly recommended.")]
        [Attributes.PropertyAttributes.PropertyRecommendedUIControl(Attributes.PropertyAttributes.PropertyRecommendedUIControl.RecommendeUIControlType.ComboBox)]
        [Attributes.PropertyAttributes.PropertyIsVariablesList(true)]
        public string v_applyToVariableName { get; set; }

        [XmlIgnore]
        [NonSerialized]
        private DataGridView ParametersGridViewHelper;

        [XmlIgnore]
        [NonSerialized]
        private Label Parameters2ndLabel;

        [XmlIgnore]
        [NonSerialized]
        private ComboBox selectionControl;

        [XmlIgnore]
        [NonSerialized]
        private List<Core.Automation.Attributes.PropertyAttributes.PropertyAddtionalParameterInfo> DGVInfo;

        public TextExtractorCommand()
        {

            this.CommandName = "TextExtractorCommand";
            this.SelectionName = "Text Extraction";
            this.CommandEnabled = true;
            this.CustomRendering = true;

            //define parameter table
            this.v_TextExtractionTable = new System.Data.DataTable
            {
                TableName = DateTime.Now.ToString("TextExtractorParamTable" + DateTime.Now.ToString("MMddyy.hhmmss"))
            };

            this.v_TextExtractionTable.Columns.Add("Parameter Name");
            this.v_TextExtractionTable.Columns.Add("Parameter Value");

            var variableProperties = this.GetType().GetProperties().Where(f => f.Name == "v_TextExtractionTable").FirstOrDefault();
            DGVInfo = variableProperties.GetCustomAttributes(typeof(Core.Automation.Attributes.PropertyAttributes.PropertyAddtionalParameterInfo), true).Cast<Core.Automation.Attributes.PropertyAttributes.PropertyAddtionalParameterInfo>().ToList();
        }

        public override void RunCommand(object sender)
        {
            //get variablized input
            var variableInput = v_InputValue.ConvertToUserVariable(sender);


            string variableLeading, variableTrailing, skipOccurences, extractedText;

            //handle extraction cases
            switch (v_TextExtractionType)
            {
                case "Extract All After Text":
                    //extract trailing texts            
                    variableLeading = GetParameterValue("Leading Text").ConvertToUserVariable(sender);
                    skipOccurences = GetParameterValue("Skip Past Occurences").ConvertToUserVariable(sender);
                    extractedText = ExtractLeadingText(variableInput, variableLeading, skipOccurences);
                    break;
                case "Extract All Before Text":
                    //extract leading text
                    variableTrailing = GetParameterValue("Trailing Text").ConvertToUserVariable(sender);
                    skipOccurences = GetParameterValue("Skip Past Occurences").ConvertToUserVariable(sender);
                    extractedText = ExtractTrailingText(variableInput, variableTrailing, skipOccurences);
                    break;
                case "Extract All Between Text":
                    //extract leading and then trailing which gives the items between
                    variableLeading = GetParameterValue("Leading Text").ConvertToUserVariable(sender);
                    variableTrailing = GetParameterValue("Trailing Text").ConvertToUserVariable(sender);
                    skipOccurences = GetParameterValue("Skip Past Occurences").ConvertToUserVariable(sender);

                    //extract leading
                    extractedText = ExtractLeadingText(variableInput, variableLeading, skipOccurences);

                    //extract trailing -- assume we will take to the first item
                    extractedText = ExtractTrailingText(extractedText, variableTrailing, "0");

                    break;
                default:
                    throw new NotImplementedException("Extraction Type Not Implemented: " + v_TextExtractionType);
            }

            //store variable
            extractedText.StoreInUserVariable(sender, v_applyToVariableName);

        }

        public override List<Control> Render(frmCommandEditor editor)
        {
            base.Render(editor);

            //ParametersGridViewHelper = new DataGridView();
            //ParametersGridViewHelper.AllowUserToAddRows = true;
            //ParametersGridViewHelper.AllowUserToDeleteRows = true;
            //ParametersGridViewHelper.Size = new Size(350, 125);
            //ParametersGridViewHelper.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            //ParametersGridViewHelper.DataBindings.Add("DataSource", this, "v_TextExtractionTable", false, DataSourceUpdateMode.OnPropertyChanged);
            //ParametersGridViewHelper = CommandControls.CreateDataGridView(this, "v_TextExtractionTable", false, false, false, 400, 160);
            //ParametersGridViewHelper.CellBeginEdit += ParameterGridViewHelper_OnCellBeginEdit;
            //ParametersGridViewHelper.CellClick += ParameterGridViewHelper_CellClick;

            //RenderedControls.AddRange(CommandControls.CreateDefaultInputGroupFor("v_InputValue", this, editor));

            //RenderedControls.Add(CommandControls.CreateDefaultLabelFor("v_TextExtractionType", this));
            //var selectionControl = (ComboBox)CommandControls.CreateDropdownFor("v_TextExtractionType", this);
            //RenderedControls.AddRange(CommandControls.CreateUIHelpersFor("v_TextExtractionType", this, new Control[] { selectionControl }, editor));
            //selectionControl.SelectionChangeCommitted += textExtraction_SelectionChangeCommitted;
            //RenderedControls.Add(selectionControl);

            ////create control for variable name
            //RenderedControls.Add(CommandControls.CreateDefaultLabelFor("v_applyToVariableName", this));
            //var VariableNameControl = CommandControls.CreateStandardComboboxFor("v_applyToVariableName", this).AddVariableNames(editor);
            //RenderedControls.AddRange(CommandControls.CreateUIHelpersFor("v_applyToVariableName", this, new Control[] { VariableNameControl }, editor));
            //RenderedControls.Add(VariableNameControl);

            //RenderedControls.Add(CommandControls.CreateDefaultLabelFor("v_TextExtractionTable", this));
            //RenderedControls.AddRange(CommandControls.CreateUIHelpersFor("v_TextExtractionTable", this, new Control[] { ParametersGridViewHelper }, editor));
            //RenderedControls.Add(ParametersGridViewHelper);

            var ctls = CommandControls.MultiCreateInferenceDefaultControlGroupFor(this, editor);
            RenderedControls.AddRange(ctls);

            ParametersGridViewHelper = (DataGridView)ctls.Where(t => t.Name == "v_TextExtractionTable").FirstOrDefault();
            ParametersGridViewHelper.CellBeginEdit += ParameterGridViewHelper_OnCellBeginEdit;
            ParametersGridViewHelper.CellClick += ParameterGridViewHelper_CellClick;

            Parameters2ndLabel = (Label)ctls.Where(t => t.Name == "lbl2_v_TextExtractionTable").FirstOrDefault();

            selectionControl = (ComboBox)ctls.Where(t => t.Name == "v_TextExtractionType").FirstOrDefault();
            selectionControl.SelectionChangeCommitted += textExtraction_SelectionChangeCommitted;

            return RenderedControls;
        }
        private void textExtraction_SelectionChangeCommitted(object sender, EventArgs e)
        {
            ComboBox extractionAction = (ComboBox)sender;

            if ((ParametersGridViewHelper == null) || (extractionAction == null) || (ParametersGridViewHelper.DataSource == null))
                return;


            var textParameters = (DataTable)ParametersGridViewHelper.DataSource;

            Parameters2ndLabel.Text = "";
            textParameters.Rows.Clear();


            switch (extractionAction.SelectedItem)
            {
                case "Extract All After Text":
                    textParameters.Rows.Add("Leading Text", "");
                    textParameters.Rows.Add("Skip Past Occurences", "0");
                    textParameters.Columns[0].ReadOnly = true;
                    break;
                case "Extract All Before Text":
                    textParameters.Rows.Add("Trailing Text", "");
                    textParameters.Rows.Add("Skip Past Occurences", "0");
                    textParameters.Columns[0].ReadOnly = true;
                    break;
                case "Extract All Between Text":
                    textParameters.Rows.Add("Leading Text", "");
                    textParameters.Rows.Add("Trailing Text", "");
                    textParameters.Rows.Add("Skip Past Occurences", "0");
                    textParameters.Columns[0].ReadOnly = true;
                    break;
                default:
                    break;
            }
        }

        private void ParameterGridViewHelper_OnCellBeginEdit(object sender, System.Windows.Forms.DataGridViewCellCancelEventArgs e)
        {
            // column A readonly
            if (e.ColumnIndex == 0)
            {
                e.Cancel = true;
            }
        }

        private void ParameterGridViewHelper_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // click colmum b, change edit mode
            if (e.ColumnIndex == 1)
            {
                ParametersGridViewHelper.BeginEdit(false);
            }
            string searchedKey = selectionControl.Text + "\t" + ParametersGridViewHelper.Rows[e.RowIndex].Cells[0].Value;

            Parameters2ndLabel.Text = CommandControls.GetDataGridViewRowInfoText(DGVInfo.Where(t => t.searchKey == searchedKey).FirstOrDefault());
        }
        private string GetParameterValue(string parameterName)
        {
            return ((from rw in v_TextExtractionTable.AsEnumerable()
                     where rw.Field<string>("Parameter Name") == parameterName
                     select rw.Field<string>("Parameter Value")).FirstOrDefault());

        }
        private string ExtractLeadingText(string input, string substring, string occurences)
        {

            //verify the occurence index
            int leadingOccurenceIndex = 0;

            if (!int.TryParse(occurences, out leadingOccurenceIndex))
            {
                throw new Exception("Invalid Index For Extraction - " + occurences);
            }

            //find index matches
            var leadingOccurencesFound = Regex.Matches(input, substring).Cast<Match>().Select(m => m.Index).ToList();

            //handle if we are searching beyond what was found
            if (leadingOccurenceIndex >= leadingOccurencesFound.Count)
            {
                throw new Exception("No value was found after skipping " + leadingOccurenceIndex + " instance(s).  Only " + leadingOccurencesFound.Count + " instances exist.");
            }

            //declare start position
            var startPosition = leadingOccurencesFound[leadingOccurenceIndex] + substring.Length;

            //substring and apply to variable
            return input.Substring(startPosition);


        }
        private string ExtractTrailingText(string input, string substring, string occurences)
        {
            //verify the occurence index
            int leadingOccurenceIndex = 0;
            if (!int.TryParse(occurences, out leadingOccurenceIndex))
            {
                throw new Exception("Invalid Index For Extraction - " + occurences);
            }

            //find index matches
            var trailingOccurencesFound = Regex.Matches(input, substring).Cast<Match>().Select(m => m.Index).ToList();

            //handle if we are searching beyond what was found
            if (leadingOccurenceIndex >= trailingOccurencesFound.Count)
            {
                throw new Exception("No value was found after skipping " + leadingOccurenceIndex + " instance(s).  Only " + trailingOccurencesFound.Count + " instances exist.");
            }

            //declare start position
            var endPosition = trailingOccurencesFound[leadingOccurenceIndex];

            //substring
            return input.Substring(0, endPosition);
        }
        public override string GetDisplayValue()
        {
            return base.GetDisplayValue() + " [Apply Extracted Text To Variable: " + v_applyToVariableName + "]";
        }
    }
}