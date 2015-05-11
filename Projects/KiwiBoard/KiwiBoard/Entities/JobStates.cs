using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KiwiBoard.Entities
{
    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class JobStates
    {

        private Jobs[] jobsField;

        private string environmentField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Jobs")]
        public Jobs[] Jobs
        {
            get
            {
                return this.jobsField;
            }
            set
            {
                this.jobsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Environment
        {
            get
            {
                return this.environmentField;
            }
            set
            {
                this.environmentField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class Jobs
    {

        private Job[] jobField;

        private string machineNameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Job")]
        public Job[] Job
        {
            get
            {
                return this.jobField;
            }
            set
            {
                this.jobField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string MachineName
        {
            get
            {
                return this.machineNameField;
            }
            set
            {
                this.machineNameField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class Job
    {

        private string dateTimeField;

        private string guidField;

        private string jobTypeField;

        private string jobSubTypeField;

        private string jobTypeSuggestionField;

        private string userNameField;

        private string clientMachineField;

        private object clientUserField;

        private string targetAPClusterField;

        private string targetVCField;

        private string targetCosmosClusterField;

        private JobRuntime runtimeField;

        private string scriptField;

        private string legacyVCField;

        private string workingDirField;

        private string friendlyLabelField;

        private object stepsField;

        private string exceptionField;

        private string scopeexeField;

        private string enableParallelismExecutionForUserField;

        private byte compilerInternalLatencyField;

        private byte compilationResultUploadLatencyField;

        private JobStage[] stagesField;

        private object classificationInfoField;

        /// <remarks/>
        public string DateTime
        {
            get
            {
                return this.dateTimeField;
            }
            set
            {
                this.dateTimeField = value;
            }
        }

        /// <remarks/>
        public string Guid
        {
            get
            {
                return this.guidField;
            }
            set
            {
                this.guidField = value;
            }
        }

        /// <remarks/>
        public string JobType
        {
            get
            {
                return this.jobTypeField;
            }
            set
            {
                this.jobTypeField = value;
            }
        }

        /// <remarks/>
        public string JobSubType
        {
            get
            {
                return this.jobSubTypeField;
            }
            set
            {
                this.jobSubTypeField = value;
            }
        }

        /// <remarks/>
        public string JobTypeSuggestion
        {
            get
            {
                return this.jobTypeSuggestionField;
            }
            set
            {
                this.jobTypeSuggestionField = value;
            }
        }

        /// <remarks/>
        public string UserName
        {
            get
            {
                return this.userNameField;
            }
            set
            {
                this.userNameField = value;
            }
        }

        /// <remarks/>
        public string ClientMachine
        {
            get
            {
                return this.clientMachineField;
            }
            set
            {
                this.clientMachineField = value;
            }
        }

        /// <remarks/>
        public object ClientUser
        {
            get
            {
                return this.clientUserField;
            }
            set
            {
                this.clientUserField = value;
            }
        }

        /// <remarks/>
        public string TargetAPCluster
        {
            get
            {
                return this.targetAPClusterField;
            }
            set
            {
                this.targetAPClusterField = value;
            }
        }

        /// <remarks/>
        public string TargetVC
        {
            get
            {
                return this.targetVCField;
            }
            set
            {
                this.targetVCField = value;
            }
        }

        /// <remarks/>
        public string TargetCosmosCluster
        {
            get
            {
                return this.targetCosmosClusterField;
            }
            set
            {
                this.targetCosmosClusterField = value;
            }
        }

        /// <remarks/>
        public JobRuntime Runtime
        {
            get
            {
                return this.runtimeField;
            }
            set
            {
                this.runtimeField = value;
            }
        }

        /// <remarks/>
        public string Script
        {
            get
            {
                return this.scriptField;
            }
            set
            {
                this.scriptField = value;
            }
        }

        /// <remarks/>
        public string legacyVC
        {
            get
            {
                return this.legacyVCField;
            }
            set
            {
                this.legacyVCField = value;
            }
        }

        /// <remarks/>
        public string WorkingDir
        {
            get
            {
                return this.workingDirField;
            }
            set
            {
                this.workingDirField = value;
            }
        }

        /// <remarks/>
        public string FriendlyLabel
        {
            get
            {
                return this.friendlyLabelField;
            }
            set
            {
                this.friendlyLabelField = value;
            }
        }

        /// <remarks/>
        public object Steps
        {
            get
            {
                return this.stepsField;
            }
            set
            {
                this.stepsField = value;
            }
        }

        /// <remarks/>
        public string Exception
        {
            get
            {
                return this.exceptionField;
            }
            set
            {
                this.exceptionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Scope.exe")]
        public string Scopeexe
        {
            get
            {
                return this.scopeexeField;
            }
            set
            {
                this.scopeexeField = value;
            }
        }

        /// <remarks/>
        public string EnableParallelismExecutionForUser
        {
            get
            {
                return this.enableParallelismExecutionForUserField;
            }
            set
            {
                this.enableParallelismExecutionForUserField = value;
            }
        }

        /// <remarks/>
        public byte CompilerInternalLatency
        {
            get
            {
                return this.compilerInternalLatencyField;
            }
            set
            {
                this.compilerInternalLatencyField = value;
            }
        }

        /// <remarks/>
        public byte CompilationResultUploadLatency
        {
            get
            {
                return this.compilationResultUploadLatencyField;
            }
            set
            {
                this.compilationResultUploadLatencyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Stage", IsNullable = false)]
        public JobStage[] Stages
        {
            get
            {
                return this.stagesField;
            }
            set
            {
                this.stagesField = value;
            }
        }

        /// <remarks/>
        public object ClassificationInfo
        {
            get
            {
                return this.classificationInfoField;
            }
            set
            {
                this.classificationInfoField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class JobRuntime
    {

        private string dereferencedField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Dereferenced
        {
            get
            {
                return this.dereferencedField;
            }
            set
            {
                this.dereferencedField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class JobStage
    {

        private string diagnosticInfoField;

        private string debuggingInfoField;

        private string nameField;

        private System.DateTime startTimeField;

        private System.DateTime endTimeField;

        private System.DateTime elapsedField;

        /// <remarks/>
        public string DiagnosticInfo
        {
            get
            {
                return this.diagnosticInfoField;
            }
            set
            {
                this.diagnosticInfoField = value;
            }
        }

        /// <remarks/>
        public string DebuggingInfo
        {
            get
            {
                return this.debuggingInfoField;
            }
            set
            {
                this.debuggingInfoField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public System.DateTime StartTime
        {
            get
            {
                return this.startTimeField;
            }
            set
            {
                this.startTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public System.DateTime EndTime
        {
            get
            {
                return this.endTimeField;
            }
            set
            {
                this.endTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "time")]
        public System.DateTime Elapsed
        {
            get
            {
                return this.elapsedField;
            }
            set
            {
                this.elapsedField = value;
            }
        }
    }
}