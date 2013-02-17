﻿using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using MetroFramework.Controls;
using MetroFramework.Native;
using TabPage = MetroFramework.Controls.TabPage;

namespace MetroFramework.Design
{
    internal class MetroTabControlDesigner : ParentControlDesigner
    {
        #region Variables
        /// <summary>
        /// 
        /// </summary>
        private readonly DesignerVerbCollection _verbs = new DesignerVerbCollection();
        /// <summary>
        /// 
        /// </summary>
        private IDesignerHost _designerHost;
        /// <summary>
        /// 
        /// </summary>
        private ISelectionService _selectionService;
        #endregion

        #region Fields
        public override SelectionRules SelectionRules
        {
            get
            {
                return Control.Dock == DockStyle.Fill ? SelectionRules.Visible : base.SelectionRules;
            }
        }
        public override DesignerVerbCollection Verbs
        {
            get
            {
                if (_verbs.Count == 2)
                {
                    var myControl = (MetroTabControl)Control;
                    _verbs[1].Enabled = myControl.TabCount != 0;
                }
                return _verbs;
            }
        }

        public IDesignerHost DesignerHost
        {
            get
            {
                return _designerHost ?? (_designerHost = (IDesignerHost)(GetService(typeof(IDesignerHost))));
            }
        }

        public ISelectionService SelectionService
        {
            get
            {
                return _selectionService ?? (_selectionService = (ISelectionService)(GetService(typeof(ISelectionService))));
            }
        }
        #endregion

        #region Constructor
        public MetroTabControlDesigner()
        {
            var verb1 = new DesignerVerb("Add Tab", OnAddPage);
            var verb2 = new DesignerVerb("Remove Tab", OnRemovePage);
            _verbs.AddRange(new[] { verb1, verb2 });
        }
        #endregion

        #region Private methods
        private void OnAddPage(Object sender, EventArgs e)
        {
            var parentControl = (MetroTabControl) Control;
            var oldTabs = parentControl.Controls;

            RaiseComponentChanging(TypeDescriptor.GetProperties(parentControl)["TabPages"]);

            var p = (TabPage) (DesignerHost.CreateComponent(typeof (TabPage)));
            p.Text = p.Name;
            parentControl.TabPages.Add(p);

            RaiseComponentChanged(TypeDescriptor.GetProperties(parentControl)["TabPages"],
                                  oldTabs, parentControl.TabPages);
            parentControl.SelectedTab = p;

            SetVerbs();
        }

        private void OnRemovePage(Object sender, EventArgs e)
        {
            var parentControl = (MetroTabControl) Control;
            var oldTabs = parentControl.Controls;

            if (parentControl.SelectedIndex < 0)
            {
                return;
            }

            RaiseComponentChanging(TypeDescriptor.GetProperties(parentControl)["TabPages"]);

            DesignerHost.DestroyComponent(parentControl.TabPages[parentControl.SelectedIndex]);

            RaiseComponentChanged(TypeDescriptor.GetProperties(parentControl)["TabPages"],
                                  oldTabs, parentControl.TabPages);

            SelectionService.SetSelectedComponents(new IComponent[]
            {
                parentControl
            }, SelectionTypes.Auto);

            SetVerbs();
        }

        private void SetVerbs()
        {
            var parentControl = (MetroTabControl) Control;

            switch (parentControl.TabPages.Count)
            {
                case 0:
                    Verbs[1].Enabled = false;
                    break;
                default:
                    Verbs[1].Enabled = true;
                    break;
            }
        }
        #endregion

        #region Overrides
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            switch (m.Msg)
            {
                case (int)WinApi.Messages.WM_NCHITTEST:
                    if (m.Result.ToInt32() == (int)WinApi.HitTest.HTTRANSPARENT)
                    {
                        m.Result = (IntPtr) WinApi.HitTest.HTCLIENT;
                    }
                    break;
            }
        }

        protected override bool GetHitTest(System.Drawing.Point point)
        {
            if (SelectionService.PrimarySelection == Control)
            {
                var hti = new WinApi.TCHITTESTINFO
                {
                    pt = Control.PointToClient(point),
                    flags = 0
                };

                var m = new Message
                {
                    HWnd = Control.Handle,
                    Msg = WinApi.TCM_HITTEST
                };

                var lparam =
                    System.Runtime.InteropServices.Marshal.AllocHGlobal(System.Runtime.InteropServices.Marshal.SizeOf(hti));
                System.Runtime.InteropServices.Marshal.StructureToPtr(hti,
                                                                      lparam, false);
                m.LParam = lparam;

                base.WndProc(ref m);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(lparam);

                if (m.Result.ToInt32() != -1)
                {
                    return hti.flags != (int)WinApi.TabControlHitTest.TCHT_NOWHERE;
                }
            }

            return false;
        }

        protected override void OnPaintAdornments(PaintEventArgs pe)
        {

        }
        #endregion
    }
}