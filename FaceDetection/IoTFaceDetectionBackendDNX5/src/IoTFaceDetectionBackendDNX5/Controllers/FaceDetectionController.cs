using IoTFaceDetectionBackendDNX5.Models;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Configuration;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace IoTFaceDetectionBackendDNX5.Controllers
{
    [Route("api/[controller]")]
    public class FaceDetectionController : Controller
    {
        public IConfiguration configuration { get; set; }
        public FaceDetectionController (IConfiguration config)
        {
            configuration = config;
        }
        
        [HttpPost]
        public async Task<VisitorResultObject> Post()
        {
            Stream req = null;
            req = Request.Body;
            byte[] bytes = null;
            MemoryStream ms = new MemoryStream();
            req.CopyTo(ms);
            bytes = ms.ToArray();

            Stream stream = new MemoryStream(bytes);
            string oxfordProjectKey = configuration.Get("AppSettings:OxfordSubscriptionKeyPrimary"); 
            FaceServiceClient faceClient = new FaceServiceClient(oxfordProjectKey);
            Face[] faceResult = null;
            faceResult = await faceClient.DetectAsync(stream);
            if (faceResult.Length == 0)
            {
                return new VisitorResultObject
                {
                    status = "Invalid",
                    totalNum = 0,
                    strangerNum = 0,
                    visitorNames = null
                };
            }
            Guid[] faceIdSet = new Guid[faceResult.Length];
            for (int i = 0; i < faceResult.Length; i ++)
            {
                faceIdSet[i] = faceResult[i].FaceId;
            }
            string memberGroupId = configuration.Get("AppSettings:MemberGroupId");
            IdentifyResult[] identityResult = null;
            try {
                identityResult = await faceClient.IdentifyAsync(memberGroupId, faceIdSet, 1);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            List<string> identifyResultName = new List<string>();

            int strangeNum = 0;
            for (int j = 0; j < identityResult.Length; j++)
            {
                if (identityResult[j].Candidates.Length == 0)
                {
                    strangeNum++;
                }
                else
                {
                    string candidateId = identityResult[j].Candidates[0].PersonId.ToString();

                    Person candidate = await faceClient.GetPersonAsync(memberGroupId, new Guid(candidateId));
                    identifyResultName.Add(candidate.Name);
                }
            }

            DateTime currentTime = DateTime.Now;
            string imageNameDate = currentTime.Year.ToString() + "Y" + currentTime.Month.ToString() + "M" + currentTime.Day.ToString() + "D" + currentTime.Hour.ToString() + "h" + currentTime.Minute.ToString() + "m" + currentTime.Second.ToString() + "s";
            string imagePath = await storageUpload("visitorcapture", imageNameDate + "_" + identifyResultName + strangeNum.ToString() + "Strangers", bytes);

            return new VisitorResultObject
            {
                status = "Valid",
                totalNum = faceResult.Length,
                strangerNum = strangeNum,
                visitorNames = identifyResultName.ToArray()
            };

        }

        public async Task<string> storageUpload(string ContainerName, string ImageName, byte[] bytes)
        {
            string storageName = configuration.Get("AppSettings:StorageAccount");
            string storageKey = configuration.Get("AppSettings:StorageKey");
            string storageCS = String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", storageName, storageKey);
            var storageAccount = CloudStorageAccount.Parse(storageCS);
            CloudBlobClient blobStorage = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobStorage.GetContainerReference(ContainerName);
            container.CreateIfNotExists();
            var permissions = container.GetPermissions();
            permissions.PublicAccess = BlobContainerPublicAccessType.Container;
            container.SetPermissions(permissions);
            string date = DateTime.Now.ToString();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(ImageName + ".jpg");
            blockBlob.Properties.ContentType = "jpg";
            await blockBlob.UploadFromByteArrayAsync(bytes, 0, bytes.Length);

            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5);
            sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(120);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Delete;
            string sasBlobToken = blockBlob.GetSharedAccessSignature(sasConstraints);
            return blockBlob.Uri + sasBlobToken;
        }        
    }
}
