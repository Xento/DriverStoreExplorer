namespace Rapr
{
    partial class ChooseDriverStore
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.radioButtonDriverStoreOnline = new System.Windows.Forms.RadioButton();
            this.radioButtonDriverStoreOffline = new System.Windows.Forms.RadioButton();
            this.textBoxOfflineStoreLocation = new System.Windows.Forms.TextBox();
            this.buttonBrowseLocation = new System.Windows.Forms.Button();
            this.radioButtonDriverStoreRemote = new System.Windows.Forms.RadioButton();
            this.textBoxRemoteComputer = new System.Windows.Forms.TextBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // radioButtonDriverStoreOnline
            // 
            this.radioButtonDriverStoreOnline.AutoSize = true;
            this.radioButtonDriverStoreOnline.Location = new System.Drawing.Point(12, 12);
            this.radioButtonDriverStoreOnline.Name = "radioButtonDriverStoreOnline";
            this.radioButtonDriverStoreOnline.Size = new System.Drawing.Size(95, 17);
            this.radioButtonDriverStoreOnline.TabIndex = 0;
            this.radioButtonDriverStoreOnline.TabStop = true;
            this.radioButtonDriverStoreOnline.Text = global::Rapr.Lang.Language.DriverStore_LocalMachine;
            this.radioButtonDriverStoreOnline.UseVisualStyleBackColor = true;
            // 
            // radioButtonDriverStoreOffline
            // 
            this.radioButtonDriverStoreOffline.AutoSize = true;
            this.radioButtonDriverStoreOffline.Location = new System.Drawing.Point(12, 35);
            this.radioButtonDriverStoreOffline.Name = "radioButtonDriverStoreOffline";
            this.radioButtonDriverStoreOffline.Size = new System.Drawing.Size(114, 17);
            this.radioButtonDriverStoreOffline.TabIndex = 1;
            this.radioButtonDriverStoreOffline.TabStop = true;
            this.radioButtonDriverStoreOffline.Text = global::Rapr.Lang.Language.DriverStore_Offline;
            this.radioButtonDriverStoreOffline.UseVisualStyleBackColor = true;
            // 
            // textBoxOfflineStoreLocation
            // 
            this.textBoxOfflineStoreLocation.Location = new System.Drawing.Point(13, 59);
            this.textBoxOfflineStoreLocation.Name = "textBoxOfflineStoreLocation";
            this.textBoxOfflineStoreLocation.Size = new System.Drawing.Size(339, 20);
            this.textBoxOfflineStoreLocation.TabIndex = 2;
            // 
            // buttonBrowseLocation
            // 
            this.buttonBrowseLocation.Location = new System.Drawing.Point(358, 56);
            this.buttonBrowseLocation.Name = "buttonBrowseLocation";
            this.buttonBrowseLocation.Size = new System.Drawing.Size(48, 23);
            this.buttonBrowseLocation.TabIndex = 3;
            this.buttonBrowseLocation.Text = "...";
            this.buttonBrowseLocation.UseVisualStyleBackColor = true;
            this.buttonBrowseLocation.Click += new System.EventHandler(this.ButtonBrowseLocation_Click);
            // 
            // radioButtonDriverStoreRemote
            // 
            this.radioButtonDriverStoreRemote.AutoSize = true;
            this.radioButtonDriverStoreRemote.Location = new System.Drawing.Point(12, 91);
            this.radioButtonDriverStoreRemote.Name = "radioButtonDriverStoreRemote";
            this.radioButtonDriverStoreRemote.Size = new System.Drawing.Size(103, 17);
            this.radioButtonDriverStoreRemote.TabIndex = 4;
            this.radioButtonDriverStoreRemote.TabStop = true;
            this.radioButtonDriverStoreRemote.Text = global::Rapr.Lang.Language.DriverStore_RemoteMachine;
            this.radioButtonDriverStoreRemote.UseVisualStyleBackColor = true;
            // 
            // textBoxRemoteComputer
            // 
            this.textBoxRemoteComputer.Location = new System.Drawing.Point(13, 115);
            this.textBoxRemoteComputer.Name = "textBoxRemoteComputer";
            this.textBoxRemoteComputer.Size = new System.Drawing.Size(393, 20);
            this.textBoxRemoteComputer.TabIndex = 5;
            // 
            // buttonOK
            // 
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(116, 150);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 6;
            this.buttonOK.Text = global::Rapr.Lang.Language.Button_OK;
            this.buttonOK.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(235, 150);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 7;
            this.buttonCancel.Text = global::Rapr.Lang.Language.Button_Cancel;
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // ChooseDriverStore
            // 
            this.AcceptButton = this.buttonOK;
            this.CancelButton = this.buttonCancel;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(418, 185);
            this.Controls.Add(this.textBoxRemoteComputer);
            this.Controls.Add(this.radioButtonDriverStoreRemote);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonBrowseLocation);
            this.Controls.Add(this.textBoxOfflineStoreLocation);
            this.Controls.Add(this.radioButtonDriverStoreOffline);
            this.Controls.Add(this.radioButtonDriverStoreOnline);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ChooseDriverStore";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = global::Rapr.Lang.Language.ChooseDriverStore_Title;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.RadioButton radioButtonDriverStoreOnline;
        private System.Windows.Forms.RadioButton radioButtonDriverStoreOffline;
        private System.Windows.Forms.TextBox textBoxOfflineStoreLocation;
        private System.Windows.Forms.Button buttonBrowseLocation;
        private System.Windows.Forms.RadioButton radioButtonDriverStoreRemote;
        private System.Windows.Forms.TextBox textBoxRemoteComputer;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
    }
}
