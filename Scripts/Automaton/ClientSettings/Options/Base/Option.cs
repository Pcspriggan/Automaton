﻿namespace CryoFall.Automaton.ClientSettings.Options
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Data;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.CBND.GameApi.ServicesClient;

    public abstract class Option<TValue> : IOptionWithValue
    {
        private readonly OptionValueHolder optionValueHolder;

        protected TValue SavedValue { get; private set; }

        protected TValue defaultValue;

        // If value is equal DefaultValue we should still process OnValueChanged.
        private bool isCurrentValueInitialized = false;

        protected Action<TValue> onValueChanged;

        protected ProtoSettings parentSettings;

        public bool IsModified => !optionValueHolder.Value.Equals(CurrentValue);

        public string Id { get; protected set; }

        public TValue CurrentValue
        {
            get => SavedValue;
            protected set
            {
                if (value.Equals(CurrentValue) && isCurrentValueInitialized)
                {
                    return;
                }

                isCurrentValueInitialized = true;

                SavedValue = value;
            }
        }

        protected Option(
            ProtoSettings parentSettings,
            string id,
            TValue defaultValue,
            Action<TValue> valueChangedCallback)
        {
            this.parentSettings = parentSettings;
            Id = id;
            SavedValue = this.defaultValue = defaultValue;
            onValueChanged = valueChangedCallback;
            optionValueHolder = new OptionValueHolder(this, SavedValue);
        }

        public virtual void Apply()
        {
            CurrentValue = optionValueHolder.Value;
            onValueChanged?.Invoke(CurrentValue);
            parentSettings.OnOptionModified(this);
        }

        public virtual void Cancel()
        {
            optionValueHolder.Value = CurrentValue;
        }

        public virtual void Reset(bool apply)
        {
            optionValueHolder.Value = defaultValue;
            if (apply)
            {
                Apply();
            }
        }

        public abstract void RegisterValueType(IClientStorage storage);

        public virtual void ApplyAbstractValue(object value)
        {
            if (value is TValue casted)
            {
                optionValueHolder.Value = casted;
                Apply();
                return;
            }

            Api.Logger.Warning(
                $"Option {Id} cannot apply abstract value - type mismatch. Will reset option to the default value");
            Reset(apply: true);
        }

        public virtual object GetAbstractValue()
        {
            return CurrentValue;
        }

        public void CreateControl(out FrameworkElement control)
        {
            CreateControlInternal(out control);
        }

        protected abstract void CreateControlInternal(out FrameworkElement control);

        protected void SetupOptionToControlValueBinding(FrameworkElement control, DependencyProperty valueProperty)
        {
            control.SetBinding(valueProperty, new Binding()
            {
                Path = new PropertyPath(nameof(optionValueHolder.Value)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            control.DataContext = optionValueHolder;
        }

        /// <summary>
        /// Option value holder is used for data binding between UI control and the option.
        /// </summary>
        protected class OptionValueHolder : INotifyPropertyChanged
        {
            private readonly Option<TValue> owner;

            private TValue value;

            public OptionValueHolder(Option<TValue> owner, TValue initialValue)
            {
                this.owner = owner;
                value = initialValue;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public TValue Value
            {
                get => value;
                set
                {
                    if (EqualityComparer<TValue>.Default.Equals(value, this.value))
                    {
                        return;
                    }

                    this.value = value;

                    // call property changed notification to notify UI about the change
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));

                    owner.parentSettings.OnOptionModified(owner);
                }
            }
        }
    }
}