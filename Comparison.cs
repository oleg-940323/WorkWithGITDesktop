using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WorkWithGITDesktop
{
    class Comparison
    {
        static string? compare;
        static ObservableCollection<ListItem> items = new ObservableCollection<ListItem>();

        /// <summary>
        /// Сравнение двух коммитов по версии устройтсва
        /// </summary>
        /// <param name="path"> Путь до репозитория </param>
        /// <param name="IDDevice"> Идентификатор устройства </param>
        /// <param name="currentVersion"> Текущая версия устройства </param>
        /// <param name="previuosVersion"> Предыдущая версия устройства </param>
        /// <param name="result"> 
        /// Результат:
        ///  -4 - Неправильно задан путь к репозиторию.
        ///  -3 - Не найден коммит с текущей версией.
        ///  -2 - Не найден коммит с предыдущей версией.
        ///  -1 - Ни одного коммита не найдено.
        ///  0 - Сравнение проведено успешно.
        /// </param>
        /// <returns> Коллекцию экземпляров класса ListItem </returns>
        public static ObservableCollection<ListItem> CompareCommits(string path, string IDDevice, string currentVersion, string previuosVersion, out int result)
        {

            // Список строк
            string[] vers;

            // Объект файла
            TreeEntry file;

            // Дискриптор репозитория
            Repository? repo = null;

            // Коммиты 
            Commit? curCommit = null, prevCommit = null;

            // Очистка списка строк
            items.Clear();

            try
            {
                repo = new Repository(path);
            }
            catch
            {
                result = -4;
                return items;
            }

            // Перебор коммитов
            foreach (Commit commit in repo.Commits)
            {
                // Получаем объект файла
                file = commit.Tree[$"Versions.csv"];

                if (file == null)
                    continue;

                // Получаем содержимое файла в виде строки
                vers = repo.Lookup<Blob>(file.Target.Sha).GetContentText().Replace("\"", "").Split(',', '\r', '\n', '\\');

                // Проверяем все ли коммиты нашли
                if (curCommit != null && prevCommit != null)
                    break;

                // Перебираем строки на нахождение типа устройства и проверки его версии
                for (int i = 0; i < vers.Length - 1; i++)
                {
                    if (vers[i] == IDDevice)
                    {
                        // Сохраняем нужные коммиты
                        if (currentVersion == vers[i + 1] && curCommit == null)
                        {
                            curCommit = commit;
                            break;
                        }
                        else if (previuosVersion == vers[i + 1] && prevCommit == null)
                        {
                            prevCommit = commit;
                            break;
                        }
                    }
                }
            }

            // Возвращаем разницу в коммитах
            if (curCommit != null && prevCommit != null)
            {
                result = 0;
                compare = repo.Diff.Compare(repo.Lookup<Blob>(curCommit.Tree[$"{IDDevice}.devdesc.xml"].Target.Sha),
                    repo.Lookup<Blob>(prevCommit.Tree[$"{IDDevice}.devdesc.xml"].Target.Sha)).Patch;
            }
            else if (curCommit != null && prevCommit == null)
            {
                result = -2;
                compare = repo.Lookup<Blob>(curCommit.Tree[$"{IDDevice}.devdesc.xml"].Target.Sha).GetContentText();
            }
            else if (curCommit == null && prevCommit != null)
            {
                result = -3;
                compare = repo.Lookup<Blob>(prevCommit.Tree[$"{IDDevice}.devdesc.xml"].Target.Sha).GetContentText();
            }
            else
            {
                result = -1;
                return items;
            }

            // Разбиваем строку на подстроки
            vers = compare.Split('\n');

            // Заполняем коллекцию
            if (result == 0)
                foreach (var str in vers)
                {
                    if (str.StartsWith("-"))
                    {
                        items.Add(new ListItem { Text = str, Color = Brushes.Red });
                    }
                    else if (str.StartsWith("+"))
                    {
                        items.Add(new ListItem { Text = str, Color = Brushes.Green });
                    }
                    else if (str.StartsWith("\t") || str.StartsWith(" "))
                    {
                        items.Add(new ListItem { Text = str, Color = Brushes.Black });
                    }
                    else
                        items.Add(new ListItem { Text = str, Color = Brushes.SkyBlue });
                }
            else
                foreach (var str in vers)
                {
                    items.Add(new ListItem { Text = str, Color = Brushes.Black });
                }

            return items;
        }
    }
}
