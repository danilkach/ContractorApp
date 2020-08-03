using System;
using System.IO;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Text;
using ContractorLibrary;

namespace ContractorApp
{
    /// <summary>
    /// Страница редактирования информации о контрагенте
    /// </summary>
    public sealed partial class addEditPage : Page
    {
        public addEditPage()
        {
            this.InitializeComponent();
            placeHolder = new BitmapImage(new Uri("ms-appx:///Assets/placeHolder.jpg"));
        }
        /// <summary>
        /// Обработчик события перехода на страницу
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            contractor = (Contractor)e.Parameter;
            if (contractor != null)
            {
                contractorEmail.Text = contractor.Email;
                contractorPhone.Text = contractor.PhoneNumber.Replace("+", "");
                contractorName.Text = contractor.Name;
                contractorImage.Source = contractor.Photo == null ? placeHolder : contractor.Photo;
            }
            else
            {
                contractorImage.Source = placeHolder;
            }
        }
        /// <summary>
        /// Обработчик события нажатия на кнопку возврата к главной странице
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "Подтверждение действия",
                Content = "Вы действительно хотите вернуться? Все несохраненные данные будут утеряны.",
                PrimaryButtonText = "ОК",
                SecondaryButtonText = "Отмена"
            };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                contractor = null;
                Frame.GoBack();
            }
        }
        /// <summary>
        /// Обработчик события нажатия на кнопку изменения изображения контрагента
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void addImageButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".jpg");
            StorageFile imageFile = await picker.PickSingleFileAsync();
            if (imageFile != null)
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                await imageFile.CopyAsync(localFolder, "tmpImage", NameCollisionOption.ReplaceExisting);
                contractorImage.Source = await XamlImageConverter.ConvertToImage(Path.Combine(
                    localFolder.Path, "tmpImage"));
            }
        }
        /// <summary>
        /// Обработчик события нажатия на кнопку удаления изображения контрагента
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void removeImageButton_Click(object sender, RoutedEventArgs e)
        {
            contractorImage.Source = placeHolder;
        }
        /// <summary>
        /// Обработчик события нажатия на кнопку сохранения информации о контрагенте 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if(contractorName.Text == string.Empty)
            {
                contractorName.Focus(FocusState.Programmatic);
                return;
            }
            if (contractorPhone.Text.Length < 11)
            {
                contractorPhone.Focus(FocusState.Programmatic);
                return;
            }
            else if (contractorPhone.Text[0] == '8')
            {
                StringBuilder builder = new StringBuilder(contractorPhone.Text);
                builder[0] = '7';
                contractorPhone.Text = builder.ToString();
            }
            contractorPhone.Text = "+" + contractorPhone.Text;
            if (contractor != null)
            {
                contractor.Name = contractorName.Text;
                contractor.Email = contractorEmail.Text;
                contractor.PhoneNumber = contractorPhone.Text;
                contractor.Photo = contractorImage.Source == placeHolder ? null : contractorImage.Source as BitmapImage;
                contractor.Photo.UriSource = new Uri(Path.Combine(ApplicationData.Current.LocalFolder.Path, "tmpImage"));
                ContractorDatabase.UpdateContractor(contractor);
            }
            else
            {
                contractor = new Contractor
                {
                    Name = contractorName.Text,
                    PhoneNumber = contractorPhone.Text,
                    Email = contractorEmail.Text,
                    Photo = contractorImage.Source == placeHolder ? null : contractorImage.Source as BitmapImage
                };
                if(contractor.Photo != null)
                    contractor.Photo.UriSource = new Uri(Path.Combine(ApplicationData.Current.LocalFolder.Path, "tmpImage"));
                ContractorDatabase.AddContractor(contractor);
            }
            Frame.GoBack();
        }
        /// <summary>
        /// Обработчик события ввода в TextBox с контактным номером контрагента;
        /// проверяет введенный символ на принадлежность к цифрам или клавиши Backspace
        /// и ограничивает длину ввода до 11 символов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contractorPhone_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!(char.IsDigit((char)e.Key) && contractorPhone.Text.Length <= 10) && e.Key != Windows.System.VirtualKey.Back)
                e.Handled = true;
        }
        /// <summary>
        /// Контейнер для placeHolder'а
        /// </summary>
        private BitmapImage placeHolder;
        /// <summary>
        /// Контрагент, подлежащий редактированию или созданию
        /// </summary>
        private Contractor contractor;
    }
}
