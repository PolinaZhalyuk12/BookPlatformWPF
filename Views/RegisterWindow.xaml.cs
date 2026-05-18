using System;
using System.Linq;
using System.Windows;

namespace BookPlatformWPF
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text) || string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Логин и пароль обязательны для заполнения!", "Ошибка");
                return;
            }

            if (Core.Context.Users.Any(u => u.Login == txtLogin.Text.Trim()))
            {
                MessageBox.Show("Пользователь с таким логином уже существует!");
                return;
            }

            var readerRole = Core.Context.Roles.FirstOrDefault(r => r.RoleName == "Читатель");

            var newUser = new Users  
            {
                Login = txtLogin.Text.Trim(),
                Password = txtPassword.Password,
                Email = txtEmail.Text?.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(txtDisplayName.Text)
                            ? txtLogin.Text
                            : txtDisplayName.Text.Trim(),
                RoleID = readerRole?.RoleID ?? 1,
                IsFrozen = false,
                CreatedAt = DateTime.Now   
            };

            Core.Context.Users.Add(newUser);
            Core.Context.SaveChanges();

            MessageBox.Show("Регистрация прошла успешно!\nТеперь вы можете войти в систему.",
                            "Регистрация завершена",
                            MessageBoxButton.OK, MessageBoxImage.Information);

            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}