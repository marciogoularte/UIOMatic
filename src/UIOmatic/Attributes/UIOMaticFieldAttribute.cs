﻿using System;

namespace UIOMatic.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class UIOMaticFieldAttribute: Attribute
    {
        public string Name { get; set; }

        public string Tab { get; set; }

        public string Description { get; set; }

        public string View { get; set; }

        public string Config { get; set; }

        public UIOMaticFieldAttribute(string name, string description)
        {
            Name = name;
            Description = description;
            View = "textfield";
        }

        public string GetView()
        {
            return this.View.StartsWith("~") 
                ? this.View 
                : string.Format("~/App_Plugins/UIOMatic/Backoffice/Views/{0}.html", View);
        }
    }
}