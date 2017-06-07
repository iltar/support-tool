﻿namespace SupportTool.AppVersion
{

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class Tools
    {

        private ToolsTool[] toolField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Tool")]
        public ToolsTool[] Tool
        {
            get
            {
                return this.toolField;
            }
            set
            {
                this.toolField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ToolsTool
    {

        private string nameField;

        private string releasePageField;

        private string latestField;

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
        public string ReleasePage
        {
            get
            {
                return this.releasePageField;
            }
            set
            {
                this.releasePageField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Latest
        {
            get
            {
                return this.latestField;
            }
            set
            {
                this.latestField = value;
            }
        }
    }
}