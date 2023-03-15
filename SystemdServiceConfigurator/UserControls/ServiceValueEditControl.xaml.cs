using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace SystemdServiceConfigurator.UserControls
{
    public sealed partial class ServiceValueEditControl : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title",
            typeof(String),
            typeof(ServiceValueEditControl),
            null);

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
            "Description",
            typeof(String),
            typeof(ServiceValueEditControl),
            null);

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value",
            typeof(String),
            typeof(ServiceValueEditControl),
            null);

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty IsSimpleProperty = DependencyProperty.Register(
            "IsSimple",
            typeof(Boolean),
            typeof(ServiceValueEditControl),
            new PropertyMetadata(false));

        public bool IsSimple
        {
            get => (bool)GetValue(IsSimpleProperty);
            set => SetValue(IsSimpleProperty, value);
        }

        public ServiceValueEditControl()
        {
            this.InitializeComponent();
        }
    }
}
