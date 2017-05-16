using Realmius.SyncService;

namespace FormsClient
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.createUserBtn = new System.Windows.Forms.Button();
            this.connectButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.usernameBox = new System.Windows.Forms.TextBox();
            this.messagesBox = new System.Windows.Forms.TextBox();
            this.messageBox = new System.Windows.Forms.TextBox();
            this.sendButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // createUserBtn
            // 
            this.createUserBtn.Location = new System.Drawing.Point(178, 10);
            this.createUserBtn.Name = "createUserBtn";
            this.createUserBtn.Size = new System.Drawing.Size(75, 23);
            this.createUserBtn.TabIndex = 0;
            this.createUserBtn.Text = "Create user";
            this.createUserBtn.UseVisualStyleBackColor = true;
            this.createUserBtn.Click += new System.EventHandler(this.CreateNewUser_Click);
            // 
            // connectButton
            // 
            this.connectButton.Location = new System.Drawing.Point(496, 9);
            this.connectButton.Name = "connectButton";
            this.connectButton.Size = new System.Drawing.Size(75, 23);
            this.connectButton.TabIndex = 1;
            this.connectButton.Text = "Connect";
            this.connectButton.UseVisualStyleBackColor = true;
            this.connectButton.Click += new System.EventHandler(this.Connect_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Username";
            // 
            // usernameBox
            // 
            this.usernameBox.Location = new System.Drawing.Point(72, 12);
            this.usernameBox.Name = "usernameBox";
            this.usernameBox.Size = new System.Drawing.Size(100, 20);
            this.usernameBox.TabIndex = 4;
            this.usernameBox.Text = "TestUser1";
            // 
            // messagesBox
            // 
            this.messagesBox.Location = new System.Drawing.Point(12, 76);
            this.messagesBox.Multiline = true;
            this.messagesBox.Name = "messagesBox";
            this.messagesBox.Size = new System.Drawing.Size(549, 136);
            this.messagesBox.TabIndex = 5;
            // 
            // messageBox
            // 
            this.messageBox.Location = new System.Drawing.Point(12, 218);
            this.messageBox.Name = "messageBox";
            this.messageBox.Size = new System.Drawing.Size(459, 20);
            this.messageBox.TabIndex = 6;
            // 
            // sendButton
            // 
            this.sendButton.Location = new System.Drawing.Point(486, 215);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new System.Drawing.Size(75, 23);
            this.sendButton.TabIndex = 7;
            this.sendButton.Text = "Save";
            this.sendButton.UseVisualStyleBackColor = true;
            this.sendButton.Click += new System.EventHandler(this.sendButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(573, 248);
            this.Controls.Add(this.sendButton);
            this.Controls.Add(this.messageBox);
            this.Controls.Add(this.messagesBox);
            this.Controls.Add(this.usernameBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.connectButton);
            this.Controls.Add(this.createUserBtn);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button createUserBtn;
        private System.Windows.Forms.Button connectButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox usernameBox;
        private System.Windows.Forms.TextBox messagesBox;
        private System.Windows.Forms.TextBox messageBox;
        private System.Windows.Forms.Button sendButton;
    }
}

