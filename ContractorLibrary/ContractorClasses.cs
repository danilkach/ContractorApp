using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using Windows.Graphics.Imaging;

namespace ContractorLibrary
{
    /// <summary>
    /// Представляет базу данных контрагентов
    /// </summary>
    public static class ContractorDatabase
    {
        /// <summary>
        /// Конструктор, инициализирующий поле, содержащее путь к локальному хранилищу приложения
        /// </summary>
        static ContractorDatabase()
        {
            DatabasePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "contractorDataBase.sql");
        }
        /// <summary>
        /// Инициализирует базу данных контрагентов
        /// </summary>
        public async static void Initialize()
        {
            await ApplicationData.Current.LocalFolder.CreateFileAsync("contractorDataBase.sql", 
                CreationCollisionOption.OpenIfExists);
            using (SqliteConnection database = new SqliteConnection($"Filename={DatabasePath}"))
            {
                database.Open();
                SqliteCommand createTable = new SqliteCommand("CREATE TABLE IF NOT EXISTS ContractorTable" +
                    "(ID INTEGER PRIMARY KEY, " +
                    "Name TEXT NOT NULL, " +
                    "Phonenumber TEXT NOT NULL, " +
                    "Email TEXT NULL, " +
                    "Photo BLOB NULL)", database);
                createTable.ExecuteReader();
            }
        }
        /// <summary>
        /// Загружает контрагентов из базы данных, возвращая объект List<Contractor>
        /// </summary>
        /// <returns></returns>
        public static List<Contractor> Load()
        {
            using (SqliteConnection database = new SqliteConnection($"Filename={DatabasePath}"))
            {
                database.Open();
                List<Contractor> result = new List<Contractor>(10);
                SqliteCommand selectCommand = new SqliteCommand("SELECT * FROM ContractorTable", database);
                SqliteDataReader reader = selectCommand.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new Contractor
                    {
                        ID = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        PhoneNumber = reader.GetString(2),
                        Email = reader.GetString(3),
                    });
                    object photo = reader.GetValue(4);
                    result.ElementAt(result.Count - 1).Photo = DBNull.Value.Equals(photo) ? 
                        null : XamlImageConverter.ConvertToImage((byte[])photo).Result;
                }
                database.Close();
                return result;
            }
        }
        /// <summary>
        /// Добавляет нового агента в базу данных
        /// </summary>
        /// <param name="c">Контрагент для добавления</param>
        public static void AddContractor(Contractor c)
        {
            using(SqliteConnection database = new SqliteConnection($"Filename={DatabasePath}"))
            {
                database.Open();
                SqliteCommand insertCommand = new SqliteCommand("INSERT INTO ContractorTable " +
                    "(Name, Phonenumber, Email, Photo)" +
                    "VALUES (@name, @phonenumber, @email, @photo)", database);
                insertCommand.Parameters.AddWithValue("@name", c.Name);
                insertCommand.Parameters.AddWithValue("@phonenumber", c.PhoneNumber);
                insertCommand.Parameters.AddWithValue("@email", c.Email);
                byte[] photo = XamlImageConverter.ConvertToByteArray(c.Photo);
                if(photo == null)
                    insertCommand.Parameters.AddWithValue("@photo", DBNull.Value);
                else
                    insertCommand.Parameters.AddWithValue("@photo", photo);
                insertCommand.ExecuteReader();
                c.ID = Convert.ToInt32(new SqliteCommand("SELECT MAX(ID) FROM ContractorTable", database).ExecuteScalar());
                database.Close();
            }
        }
        /// <summary>
        /// Обновляет данные о контрагенте, присутствующем в базе данных 
        /// </summary>
        /// <param name="c">Объект контрагента, данные которого подлежат обновлению</param>
        public static void UpdateContractor(Contractor c)
        {
            using (SqliteConnection database = new SqliteConnection($"Filename={DatabasePath}"))
            {
                database.Open();
                SqliteCommand updateCommand = new SqliteCommand("UPDATE ContractorTable SET Name=@name, Phonenumber=@phonenumber," +
                    " Email=@email,  Photo=@photo WHERE ID=@id", database);
                updateCommand.Parameters.AddWithValue("@id", c.ID);
                updateCommand.Parameters.AddWithValue("@name", c.Name);
                updateCommand.Parameters.AddWithValue("@phonenumber", c.PhoneNumber);
                updateCommand.Parameters.AddWithValue("@email", c.Email);
                byte[] photo = XamlImageConverter.ConvertToByteArray(c.Photo);
                if (photo == null)
                    updateCommand.Parameters.AddWithValue("@photo", DBNull.Value);
                else
                    updateCommand.Parameters.AddWithValue("@photo", photo);
                updateCommand.ExecuteReader();
                database.Close();
            }
        }
        /// <summary>
        /// Удаляет контрагента, присутсвующего в базе
        /// </summary>
        /// <param name="id">ID контрагента, подлежащего удалению</param>
        public static void RemoveContractor(int id)
        {
            using (SqliteConnection database = new SqliteConnection($"Filename={DatabasePath}"))
            {
                database.Open();
                SqliteCommand command = new SqliteCommand($"DELETE FROM ContractorTable WHERE ID = {id}", database);
                command.ExecuteReader();
                database.Close();
            }
        }
        /// <summary>
        /// Поле, содержащее информацию о пути к локальному хранилищу приложения
        /// </summary>
        public static string DatabasePath;
    }
    /// <summary>
    /// Класс контрагента
    /// </summary>
    public class Contractor
    {
        /// <summary>
        /// Уникальный идентификатор контрагента;
        /// устанавливается классом ContractorDatabase
        /// </summary>
        public int ID { get; internal set; }
        /// <summary>
        /// Имя контрагента
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Контактный номер контрагента
        /// </summary>
        public string PhoneNumber { get; set; }
        /// <summary>
        /// Email контрагента
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// Фотография контрагента
        /// </summary>
        public BitmapImage Photo { get; set; }
    }
    /// <summary>
    /// Класс, преобразующий файл изображения и 
    /// Windows.UI.Xaml.Media.Imaging.BitmapImage в byte[] и наоборот
    /// </summary>
    public static class XamlImageConverter
    {
        /// <summary>
        /// Преобразует Windows.UI.Xaml.Media.Imaging.BitmapImage в byte[]
        /// </summary>
        /// <param name="image">Windows.UI.Xaml.Media.Imaging.BitmapImage подлежащий преобразованию</param>
        /// <returns>Возвращает объект типа Task<byte[]></returns>
        public static byte[] ConvertToByteArray(BitmapImage image)
        {
            if (image == null)
                return null;
            byte[] result = File.ReadAllBytes(image.UriSource.AbsolutePath);
            return result;
        }
        /// <summary>
        /// Преобразует byte[] в Windows.UI.Xaml.Media.Imaging.BitmapImage
        /// </summary>
        /// <param name="imageBytes">byte[] подлежащий преобразованию</param>
        /// <returns>Возвращает объект типа Task<Windows.UI.Xaml.Controls.Image></returns>
        public async static Task<BitmapImage> ConvertToImage(byte[] imageBytes)
        {
            if (imageBytes == null)
                return null;
            BitmapImage image = new BitmapImage();
            using (InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream())
            {
                using (DataWriter writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(imageBytes);
                    await writer.StoreAsync();
                }
                image.SetSource(randomAccessStream);
            }
            return image;
        }
        /// <summary>
        /// Преобразует файл изображения в Windows.UI.Xaml.Media.Imaging.BitmapImage
        /// </summary>
        /// <param name="imageBytes">Путь к файлу, подлежащему преобразованию</param>
        /// <returns>Возвращает объект типа Task<Windows.UI.Xaml.Controls.Image></returns>
        public async static Task<BitmapImage> ConvertToImage(string filename)
        {
            if (filename == null)
                return null;
            BitmapImage image = new BitmapImage();
            byte[] imageBytes = File.ReadAllBytes(filename);
            using (InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream())
            {
                using(DataWriter writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(imageBytes);
                    await writer.StoreAsync();
                }
                await image.SetSourceAsync(randomAccessStream);
            }
            return image;
        }
    }
}
