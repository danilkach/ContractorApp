using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI;
using ContractorLibrary;

namespace ContractorApp
{
    /// <summary>
    /// Главная страница, предназначенная для просмотра информации о контрагентах,
    /// создания, редактирования и удаления записей контрагетов в базе данных
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        /// <summary>
        /// Обработчик события загрузки главной страницы; загружает контрагентов
        /// из базы данных и вызывает метод отрисовки AddContractorPanel()
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            lastClickedContractor = null;
            ContractorDatabase.Initialize();
            contractors = ContractorDatabase.Load();
            placeHolder = new BitmapImage(new Uri("ms-appx:///Assets/placeHolder.jpg"));
            rowIndex = 0;
            foreach (Contractor c in contractors)
                AddContractorPanel(c);
            ResizePageContent();
        }
        /// <summary>
        /// Обработчик события нажатия на панель с контрагентом; выделяет выбранную панель #25fc82
        /// и выводит подробную информацию в контейнер, расположенный в левой части приложения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contractorPressedEvent(object sender, PointerRoutedEventArgs e)
        {
            if (lastClickedContractor != sender as RelativePanel)
            {
                (sender as RelativePanel).Background = new SolidColorBrush(Color.FromArgb(255, 37, 252, 130));
                if (lastClickedContractor != null)
                    lastClickedContractor.Background = new SolidColorBrush(Colors.White);
                lastClickedContractor = (sender as RelativePanel);
            }
            Contractor pickedContractor = contractors.Find((c) => c.ID == (int)(sender as RelativePanel).Tag);
            contractorPhoto.Source = pickedContractor.Photo == null ?
                placeHolder : pickedContractor.Photo;
            contractorName.Text = pickedContractor.Name;
            contractorPhonenumberLabel.Text = pickedContractor.PhoneNumber;
            contractorEmailLabel.Text = pickedContractor.Email;
            contractorInfo.Tag = pickedContractor.ID;
            contractorInfo.Visibility = Visibility.Visible;
            ResizePageContent();
        }
        /// <summary>
        /// Обработчик события изменения размера страницы; вызывает ResizePageContent
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizePageContent();
        }
        /// <summary>
        /// Изменяет размеры контейнеров, всех кнопок и панелей с контрагентами
        /// </summary>
        private void ResizePageContent()
        {
            foreach (UIElement u in contractorsList.Children)
            {
                RelativePanel panel = u as RelativePanel;
                panel.Width = mainGrid.ColumnDefinitions[1].ActualWidth - panel.Margin.Left - panel.Margin.Right
                    - contractorsScrollViewer.BorderThickness.Left * 2 - contractorsPanel.BorderThickness.Left * 2;
            }
            editContractorButton.Width = mainGrid.ColumnDefinitions[0].ActualWidth / 2
                - editContractorButton.Margin.Right - editContractorButton.Margin.Left;
            deleteContractorButton.Width = mainGrid.ColumnDefinitions[0].ActualWidth / 2
                - deleteContractorButton.Margin.Left - deleteContractorButton.Margin.Right;
            newContractorButton.Width = mainGrid.ColumnDefinitions[1].ActualWidth
                - newContractorButton.Margin.Left * 2;
            deleteEditButtonsPanel.Width = mainGrid.ColumnDefinitions[0].ActualWidth;
            contractorInfoPanel.Width = mainGrid.ColumnDefinitions[0].ActualWidth;
        }
        /// <summary>
        /// Создает, отрисовывает и добавляет в контейнер панель с информацией о контрагенте
        /// </summary>
        /// <param name="c"></param>
        private void AddContractorPanel(Contractor c)
        {
            RelativePanel panel = new RelativePanel();
            Image contractorImage = new Image();
            contractorImage.Source = c.Photo ?? placeHolder;
            contractorImage.MaxWidth = contractorImage.MaxHeight = 50;
            contractorImage.Margin = new Thickness(5, 5, 5, 5);
            TextBlock contractorName = new TextBlock();
            contractorName.Text = c.Name;
            contractorName.VerticalAlignment = VerticalAlignment.Center;
            contractorName.HorizontalAlignment = HorizontalAlignment.Stretch;
            contractorName.TextWrapping = TextWrapping.WrapWholeWords;
            RelativePanel.SetRightOf(contractorName, contractorImage);
            panel.Margin = new Thickness(2, 5, 2, 5);
            panel.BorderBrush = new SolidColorBrush(Colors.Black);
            panel.BorderThickness = new Thickness(3);
            panel.MaxHeight = 60;
            panel.Width = mainGrid.ColumnDefinitions[1].ActualWidth - panel.Margin.Right;
            panel.PointerPressed += contractorPressedEvent;
            panel.Tag = c.ID;
            panel.Children.Add(contractorImage);
            panel.Children.Add(contractorName);
            panel.Background = new SolidColorBrush(Colors.White);
            RowDefinition newRow = new RowDefinition();
            newRow.MaxHeight = 70;
            contractorsList.RowDefinitions.Add(newRow);
            Grid.SetRow(panel, rowIndex++);
            contractorsList.Children.Add(panel);
        }
        /// <summary>
        /// Обработчик события нажатия на кнопку удаления контрагента;
        /// удаляет визуальное представление выбранного контрагента и его запись из базы данных
        /// с предварительным вызовом диалогового окна подтверждения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void deleteContractorButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "Подтверждение действия",
                Content = "Вы действительно хотите удалить выбранного контрагента?",
                PrimaryButtonText = "ОК",
                SecondaryButtonText = "Отмена"
            };
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                Contractor contractorToDelete = contractors.Find((c) => c.ID == (int)contractorInfo.Tag);
                RelativePanel panelToDelete = contractorsList.Children.First(
                    (c) => (int)(c as RelativePanel).Tag == contractorToDelete.ID) as RelativePanel;
                contractorsList.Children.Remove(panelToDelete);
                contractorsList.RowDefinitions[Grid.GetRow(panelToDelete)].Height = new GridLength(0);
                ContractorDatabase.RemoveContractor(contractorToDelete.ID);
                contractors.Remove(contractorToDelete);
                lastClickedContractor = null;
                contractorInfo.Visibility = Visibility.Collapsed;
            }
        }
        /// <summary>
        /// Обработчик события нажатия на кнопку редактирования контрагента;
        /// переходит на страницу редактирования/создания с передачей информации о редактируемом контрагенте
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editContractorButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(addEditPage), contractors.Find((c) => c.ID == (int)lastClickedContractor.Tag));
        }
        /// <summary>
        /// Обработчик события нажатия на кнопку добавления контрагента;
        /// переходит на страницу редактирования/создания с указанием создания нового контрагента
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newContractorButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(addEditPage), null);
        }

        /// <summary>
        /// Индекс последней добавленой строки в Grid, хранящий панели с контрагентами 
        /// </summary>
        private int rowIndex;
        /// <summary>
        /// Последняя выбранная панель контрагента
        /// </summary>
        private RelativePanel lastClickedContractor;
        /// <summary>
        /// Список контрагентов, загруженных из базы данных
        /// </summary>
        private List<Contractor> contractors;
        /// <summary>
        /// Контейнер для placeHolder'а
        /// </summary>
        private BitmapImage placeHolder;
    }
}
