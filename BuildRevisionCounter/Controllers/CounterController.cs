﻿using BuildRevisionCounter.Model;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
namespace BuildRevisionCounter.Controllers
{
	[RoutePrefix("api/counter")]
	public class CounterController : ApiController
	{
		private static MongoDBStorage _storage;

		static CounterController()
		{
			_storage = new MongoDBStorage();
		}

		[HttpGet]
		[Route("{revisionName}")]
		public long Current([FromUri] string revisionName)
		{
			var q = Query<RevisionModel>.Where(_ => _.Id == revisionName);
			var revision = _storage.Revisions.FindOne(q);

			if (revision == null)
				throw new HttpResponseException(HttpStatusCode.NotFound);

			return revision.NextNumber;
		}

		[HttpPost]
		[Route("{revisionName:regex([A-Za-z0-9._%+-]*)}")]
		public long Bumping([FromUri] string revisionName)
		{
			var result = _storage.Revisions.FindAndModify(new FindAndModifyArgs()
			{
				Query = Query<RevisionModel>.Where(_ => _.Id == revisionName),
				Upsert = true,
				Update = Update<RevisionModel>
					.SetOnInsert(_ => _.Created, DateTime.UtcNow)
					.Inc(_ => _.NextNumber, 1)
					.Set(_ => _.Updated, DateTime.UtcNow),
				VersionReturned = FindAndModifyDocumentVersion.Modified
			});

			var revision = result.GetModifiedDocumentAs<RevisionModel>();

			return revision.NextNumber;
		}
	}
}