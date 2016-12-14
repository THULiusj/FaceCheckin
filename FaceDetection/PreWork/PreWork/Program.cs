using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PreWork
{
    class Program
    {
        static void Main(string[] args)
        {
            //Input oxfordKey
            Console.WriteLine("Please input the OxfordPrimaryKey: ");
            string oxfordProjectKey = Console.ReadLine();
            FaceServiceClient faceClient = new FaceServiceClient(oxfordProjectKey);

            //Create PersonGroup
            string groupName = "";
            string groupId = "";
            Console.WriteLine("Create a new Person Group? [Y/N]");
            string personGroupChoice = Console.ReadLine();
            if (personGroupChoice == "Y")
            {
                Console.WriteLine("Please input the PersonGroup Name: ");
                groupName = Console.ReadLine();
                groupId = Guid.NewGuid().ToString();
                var runSync = Task.Factory.StartNew(new Func<Task>(async () =>
                {
                    await faceClient.CreatePersonGroupAsync(groupId, groupName);
                })).Unwrap();
                runSync.Wait();
            }
            else
            {
                Console.WriteLine("Please input the PersonGroup Id: ");
                groupId = Console.ReadLine();
            }

            Console.WriteLine("Adding person and his photos......");
            //Add Persons and Photos
            DirectoryInfo dirPrograms = new DirectoryInfo(Environment.CurrentDirectory);
            List<DirectoryInfo> dirs = new List<DirectoryInfo>(dirPrograms.EnumerateDirectories());
            foreach(DirectoryInfo dirsplit in dirs)
            {
                string lastName = dirsplit.Name.Substring(dirsplit.Name.IndexOf("_") + 1, dirsplit.Name.Length - dirsplit.Name.IndexOf("_") - 1);
                string firstName = dirsplit.Name.Substring(0, dirsplit.Name.IndexOf("_"));
                //Create Person
                CreatePersonResult personResult = null;
                var runSync = Task.Factory.StartNew(new Func<Task>(async () =>
                {
                    personResult = await faceClient.CreatePersonAsync(groupId, firstName + " " + lastName);
                })).Unwrap();
                runSync.Wait();
                Console.WriteLine("Creating " + firstName + " " + lastName);
                //Add photos
                List <FileInfo> files = new List<FileInfo>(dirsplit.EnumerateFiles());
                foreach(FileInfo filesplit in files)
                {
                    FileStream fs0 = new FileStream(filesplit.Directory + "\\" + filesplit.Name, FileMode.Open);
                    byte[] bytes = new byte[fs0.Length];
                    fs0.Read(bytes, 0, bytes.Length);
                    fs0.Close();
                    Stream imageStream = new MemoryStream(bytes);
                    AddPersistedFaceResult perFaceResult = null;
                    runSync = Task.Factory.StartNew(new Func<Task>(async () =>
                    {
                        perFaceResult = await faceClient.AddPersonFaceAsync(groupId, personResult.PersonId, imageStream);
                    })).Unwrap();
                    runSync.Wait();
                }
            }

            //Train and get training status
            faceClient.TrainPersonGroupAsync(groupId);
            TrainingStatus trStatus = null;
            do
            {
                Console.WriteLine("Waiting for training.");
                Thread.Sleep(3000);
                var runSync = Task.Factory.StartNew(new Func<Task>(async () =>
                {
                    trStatus = await faceClient.GetPersonGroupTrainingStatusAsync(groupId);
                })).Unwrap();
                runSync.Wait();


            } while (trStatus == null || trStatus.Status == Status.Running);

            Console.WriteLine("TrainingStatus: " + trStatus.Status.ToString());

            //Write the info to txt file
            string data1 = "oxfordKey: " + oxfordProjectKey;
            string data2 = "PersonGroupId: " + groupId;
            StreamWriter sw = new StreamWriter("OxfordData.txt", false, Encoding.Default);
            sw.WriteLine(data1);
            sw.WriteLine(data2);
            sw.Close();

            Console.ReadLine();
        }
    }
}
