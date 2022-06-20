using System;
using System.Text.RegularExpressions;
using Npgsql;
using Dapper;
using api.DataAccess;
using api.DataAccess.Models;
using System.Threading.Tasks;
using System.Data;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace db_sample_seeder
{
	class Program
	{

		static async Task Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			await Program.Connection();
		}

		public static async Task<long> CreateSampleArticle(
			NpgsqlConnection db,
			string title,
			Source source,
			string description,
			string articleURL,
			string imageURL,
			UserAccount importer
		)
		{

			var daysAgoPublished = RandomNumberGenerator.GetInt32(1, 20);
			var article = db.CreateArticle(
						title,
						FullSlug(source.Slug, title),
						sourceId: source.Id,
						datePublished: DateTime.Now.AddDays(-daysAgoPublished),
						dateModified: null,
						section: "",
						description,
						authors: new AuthorMetadata[1] { new AuthorMetadata("Mega Writer", "https://website.com/mega-writer", "mega-writer") },
						tags: new TagMetadata[0] { }
					);

			int wordCount = RandomNumberGenerator.GetInt32(200, 2300);

			db.CreatePage(article, 1, wordCount, wordCount, articleURL);

			await db.SetArticleImage(article, importer.Id, imageURL);

			// Make the user read the article
			var userArticle = await db.CreateUserArticle(article, importer.Id, wordCount, true, new api.Analytics.ClientAnalytics());
			db.UpdateReadProgress(userArticle.Id, new int[1] { wordCount }, new api.Analytics.ClientAnalytics());

			return article;
		}

		static async Task Connection()
		{
			Program.InitalizeNpgsqlMappings();

			var connString = "Host=host.docker.internal;Username=postgres;Password=postgres;Database=rrit";
			using (var db = new NpgsqlConnection(connString))
			{
				// Create a user
				var salt = GenerateSalt();
				var password = "password";
				var user = await db.CreateUserAccount("PrimordialReader", "sample@email.com", HashPassword(password, salt), salt, 347, DisplayTheme.Light, new UserAccountCreationAnalytics());

				// Create a website source
				var source = db.CreateSource("Best website ever", "https://website.com", "website.com", "best-website-ever");

				var article1 = await Program.CreateSampleArticle(db,
					"The White Swamphen Revealed",
					source,
					"The white swamphen (Porphyrio albus) was a rail found on Lord Howe Island, east of the Australian mainland. All contemporary accounts and illustrations were produced between 1788 and 1790, when the bird was first encountered by British ship crews.",
					"https://website.com/the-white-swamphen",
					"https://upload.wikimedia.org/wikipedia/commons/thumb/9/9c/Liverpool_white_swamphen.jpg/136px-Liverpool_white_swamphen.jpg",
					user);

				await db.CreateComment("It was a good read!", article1, null, user.Id, new api.Analytics.ClientAnalytics());

				var article2 = await Program.CreateSampleArticle(db,
					"The War On The Pronunciation of GIF",
					source,
					"The pronunciation of GIF has been disputed since the 1990s. GIF, an acronym for the Graphics Interchange Format, is popularly pronounced in English as a one-syllable word.",
					"https://website.com/gif-pronunciation",
					"https://upload.wikimedia.org/wikipedia/commons/thumb/7/72/Stephen_Webby_slide_at_the_2013_Webby_Awards.jpg/171px-Stephen_Webby_slide_at_the_2013_Webby_Awards.jpg",
					user);

				await db.CreateComment("It was a decent read!", article2, null, user.Id, new api.Analytics.ClientAnalytics());

				// Run the scoring of articles
				await db.ExecuteAsync(
					sql: "article_api.score_articles",
					commandType: CommandType.StoredProcedure
				);

				// Set an AOTD
				await db.SetAotd();

				var article3 = await Program.CreateSampleArticle(db,
					"The Story Behind The Song",
					source,
					"\"I've Just Seen a Face\" is a Beatles song written and sung by Paul McCartney (pictured), first released on the album Help! in August 1965. A cheerful ballad of love at first sight, it may have been inspired by McCartney's relationship with actress Jane Asher.",
					"https://website.com/the-story-behind-the-song",
					"https://upload.wikimedia.org/wikipedia/commons/thumb/5/5c/Paul_McCartney_with_Linda_McCartney_-_Wings_-_1976.jpg/171px-Paul_McCartney_with_Linda_McCartney_-_Wings_-_1976.jpg",
					user);

				await db.CreateComment("It was a decent read!", article3, null, user.Id, new api.Analytics.ClientAnalytics());

				// Run the scoring of articles again, to populate the Contenders.
				await db.ExecuteAsync(
					sql: "article_api.score_articles",
					commandType: CommandType.StoredProcedure
				);

				// Set a second AOTD, so we get AOTD history of at least 2 items for the homepage UI
				await db.SetAotd();

			}

		}

		private static void InitalizeNpgsqlMappings()
		{
			NpgsqlConnection.GlobalTypeMapper.MapEnum<ArticleFlair>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<AuthorContactStatus>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<AuthServiceProvider>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<AuthorAssignmentMethod>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<BulkEmailSubscriptionStatusFilter>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<DisplayTheme>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<FreeTrialCreditTrigger>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<FreeTrialCreditType>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<SourceRuleAction>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<UserAccountRole>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<NotificationChannel>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<NotificationAction>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<NotificationEventFrequency>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<NotificationEventType>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<NotificationPushUnregistrationReason>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<SubscriptionEnvironment>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<SubscriptionEventSource>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<SubscriptionPaymentMethodBrand>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<SubscriptionPaymentMethodWallet>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<SubscriptionPaymentStatus>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<SubscriptionProvider>();
			NpgsqlConnection.GlobalTypeMapper.MapEnum<TwitterHandleAssignment>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<ArticleAuthor>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<AuthorContactStatusAssignment>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<AuthorMetadata>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<CommentAddendum>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<PostReference>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<Ranking>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<StreakRanking>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<SubscriptionDistributionAuthorCalculation>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<SubscriptionDistributionAuthorReport>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<SubscriptionStatusLatestPeriod>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<SubscriptionStatusLatestRenewalStatusChange>();
			NpgsqlConnection.GlobalTypeMapper.MapComposite<TagMetadata>();
		}

		static string FullSlug(string sourceSlug, string articleTitle)
		{
			return sourceSlug + "_" + CreateSlug(articleTitle);
		}

		// Below private methods are copy-pasted from the api repository
		// should they be extracted into a shared utility package? 
		// ----------------------------------------------------------------------------------------
		private static string CreateSlug(string value)
		{
			var slug = Regex.Replace(Regex.Replace(value, @"[^a-zA-Z0-9-\s]", ""), @"\s", "-").ToLower();
			return slug.Length > 80 ? slug.Substring(0, 80) : slug;
		}

		private static byte[] GenerateSalt()
		{
			var salt = new byte[128 / 8];
			using (var rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(salt);
			}
			return salt;
		}
		private static byte[] HashPassword(string password, byte[] salt) => KeyDerivation.Pbkdf2(
			password: password,
			salt: salt,
			prf: KeyDerivationPrf.HMACSHA1,
			iterationCount: 10000,
			numBytesRequested: 256 / 8);
	}
}
