using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NOTFGT.FLZ_Common.GUI.Logic
{
    internal abstract class UIElement
    {
        protected static GUIManager GUI => FLZ_ToolsManager.Instance.GUIUtil;
        internal delegate void OnElementStateChange(bool isActive, bool wasActive);
        internal OnElementStateChange StateChangeCallback;
        protected GameObject CoreObject { get; set; }
        bool _isActive;
        internal bool IsActive
        {
            get => _isActive;
            set
            {
                var wasActive = CoreObject.gameObject.activeInHierarchy;
                _isActive = value;
                CoreObject.gameObject.SetActive(_isActive);
                StateChangeCallback?.Invoke(_isActive, wasActive);
            }
        }
        protected abstract void Initialize();
        protected abstract void StateChange(bool isActive, bool wasActive);
    }
}
