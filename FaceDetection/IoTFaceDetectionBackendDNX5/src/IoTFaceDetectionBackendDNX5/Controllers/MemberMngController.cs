using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Configuration;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IoTFaceDetectionBackendDNX5.Controllers
{
    [Route("api/[controller]")]
    public class MemberMngController : Controller
    {
        public IConfiguration Configuration { get; set; }
        public MemberMngController(IConfiguration config)
        {
            Configuration = config;
        }        
        [HttpPost]
        public async Task<string> Post(string lastName, string firstName, string permission) //Create a member
        {
            FaceServiceClient faceClient = new FaceServiceClient(Configuration.Get("AppSettings:OxfordSubscriptionKeyPrimary"));
            string memberGroupId = Configuration.Get("AppSettings:OxfordSubscriptionKeyPrimary");
            CreatePersonResult memberDetail = await faceClient.CreatePersonAsync(memberGroupId, lastName + " " + firstName, permission);
            return memberDetail.PersonId.ToString();
        }
        [HttpPut]
        public async Task<string> Put(string MemberId) //Upload a member's picture
        {
            Stream req = null;
            req = Request.Body;
            byte[] bytes = null;
            MemoryStream ms = new MemoryStream();
            req.CopyTo(ms);
            //int count = 0;
            //do
            //{
            //    byte[] buf = new byte[1024];
            //    count = req.Read(buf, 0, 1024);
            //    ms.Write(buf, 0, count);
            //} while (req.CanRead && count > 0);
            bytes = ms.ToArray();
            Stream stream = new MemoryStream(bytes);
            FaceServiceClient faceClient = new FaceServiceClient(Configuration.Get("AppSettings:OxfordSubscriptionKeyPrimary"));
            string memberGroupId = Configuration.Get("AppSettings:MemberGroupId");
            await faceClient.AddPersonFaceAsync(memberGroupId, new Guid(MemberId), stream);
            string blobUri = await storageUpload("memberpic", MemberId, bytes);
            return blobUri;
        }

        [HttpDelete]
        public async Task Delete(string MemberId) // Delete a member
        {
            FaceServiceClient faceClient = new FaceServiceClient(Configuration.Get("AppSettings:OxfordSubscriptionKeyPrimary"));
            string memberGroupId = Configuration.Get("AppSettings:OxfordSubscriptionKeyPrimary");
            await faceClient.DeletePersonAsync(memberGroupId, new Guid(MemberId));
            return;
        }

        [HttpGet]
        public async Task<Person[]> Get() //Get all members
        {
            FaceServiceClient faceClient = new FaceServiceClient(Configuration.Get("AppSettings:OxfordSubscriptionKeyPrimary"));
            Person[] memberList = await faceClient.GetPersonsAsync(Configuration.Get("AppSettings:MemberGroupId"));
            return memberList;
        }
        [HttpPatch]
        public async Task Patch() //TrainMembers
        {
            FaceServiceClient faceClient = new FaceServiceClient(Configuration.Get("AppSettings:OxfordSubscriptionKeyPrimary"));
            await faceClient.TrainPersonGroupAsync(Configuration.Get("AppSettings:OxfordSubscriptionKeyPrimary"));
            return;
        }
        public async Task<string> storageUpload(string ContainerName, string ImageName, byte[] bytes)
        {
            var storageAccount = CloudStorageAccount.Parse(Configuration.Get("AppSettings:StorageConnectionString"));
            CloudBlobClient blobStorage = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobStorage.GetContainerReference(ContainerName);
            container.CreateIfNotExists();
            var permissions = container.GetPermissions();
            permissions.PublicAccess = BlobContainerPublicAccessType.Container;
            container.SetPermissions(permissions);
            CloudBlockBlob blockBlob = null;
            for (int i = 0; ; i++)
            {
                blockBlob = container.GetBlockBlobReference(ImageName + "_" + i.ToString() + ".jpg");
                if (blockBlob.Exists())
                    continue;
                else
                    break;
            }
            blockBlob.Properties.ContentType = "jpg";
            await blockBlob.UploadFromByteArrayAsync(bytes, 0, bytes.Length);
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5);
            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Delete;
            string sasBlobToken = blockBlob.GetSharedAccessSignature(sasConstraints);
            return blockBlob.Uri + sasBlobToken;
        }
    }
}
