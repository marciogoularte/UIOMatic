﻿using System;

namespace UIOMatic.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class UIOMaticListViewFieldAttribute : Attribute
    {
        public string Name { get; set; }

        public string View { get; set; }

        public string Config { get; set; }

        public UIOMaticListViewFieldAttribute(string name)
        {
            Name = name;
            View = "label";
        }

        public string GetView()
        {
            return View.StartsWith("~") 
                ? View 
                : string.Format("~/App_Plugins/UIOMatic/Backoffice/Views/{0}.html", View);
        }
    }
}