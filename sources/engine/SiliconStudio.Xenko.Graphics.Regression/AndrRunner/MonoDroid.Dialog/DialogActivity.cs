// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Widget;

namespace MonoDroid.Dialog
{
    public class DialogInstanceData : Java.Lang.Object
    {
        public DialogInstanceData()
        {
            _dialogState = new Dictionary<string, string>();
        }

        private Dictionary<String, String> _dialogState;
    }

	public class DialogActivity : ListActivity
	{
		public RootElement Root { get; set; }
        private DialogHelper Dialog { get; set; }
		
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
            Dialog = new DialogHelper(this, this.ListView, this.Root);

            if (this.LastNonConfigurationInstance != null)
            {
                // apply value changes that are saved
            }
        }

        public override Java.Lang.Object OnRetainNonConfigurationInstance()
        {
            return null;
        }
		
		public void ReloadData()
		{
			if(Root == null) {
				return;
			}
			
			this.Dialog.ReloadData();
			
			
		}
	}
}