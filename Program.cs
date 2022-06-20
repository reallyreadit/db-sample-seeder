using System;
using System.Text.RegularExpressions;
using Npgsql;
using Dapper;
using api.DataAccess;
using api.DataAccess.Models;
using System.Threading.Tasks;
using System.Data;

namespace db_sample_seeder
{
	class Program
	{

		static async Task Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			await Program.Connection();
		}

		static async Task Connection()
		{
			Program.InitalizeNpgsqlMappings();

			var connString = "Host=host.docker.internal;Username=postgres;Password=postgres;Database=rrit";
			using (var db = new NpgsqlConnection(connString))
			{
				// Create a user
				var user = await db.CreateUserAccount("PrimordialReader", "sample@email.com", new byte[0] { }, new byte[0] { }, 347, DisplayTheme.Light, new UserAccountCreationAnalytics());

				// Create a website source
				var source = db.CreateSource("Best website ever", "https://website.com", "website.com", "best-website-ever");

				// Create article 1
				var article = db.CreateArticle(
							"Great Article",
							"best-website-ever_article-one",
							sourceId: source.Id,
							datePublished: DateTime.Now.AddDays(-5),
							dateModified: null,
							section: "",
							description: "This might just be the best article ever",
							authors: new AuthorMetadata[1] { new AuthorMetadata("Mega Writer", "https://website.com/mega-writer", "mega-writer") },
							tags: new TagMetadata[0] { }
						);

				db.CreatePage(article, 1, 500, 500, "https://website.com/article_one");

				await db.SetArticleImage(article, user.Id, "https://randomwordgenerator.com/img/picture-generator/57e7d7444955ac14f1dc8460962e33791c3ad6e04e50744172297cd59344c5_640.jpg");

				var userArticle = await db.CreateUserArticle(article, user.Id, 500, true, new api.Analytics.ClientAnalytics());
				db.UpdateReadProgress(userArticle.Id, new int[1] { 500 }, new api.Analytics.ClientAnalytics());

				await db.CreateComment("It was a good read!", article, null, user.Id, new api.Analytics.ClientAnalytics());

				// article 2 
				var article2 = db.CreateArticle(
						"Second best",
						"best-website-ever_article-two",
						sourceId: source.Id,
						datePublished: DateTime.Now.AddDays(-2),
						dateModified: null,
						section: "",
						description: "This might just be the best article ever",
						authors: new AuthorMetadata[1] { new AuthorMetadata("Mega Writer", "https://website.com/mega-writer", "mega-writer") },
						tags: new TagMetadata[0] { }
					);
				db.CreatePage(article2, 1, 400, 400, "https://website.com/article_two");
				await db.SetArticleImage(article2, user.Id, "https://randomwordgenerator.com/img/picture-generator/57e7d7444955ac14f1dc8460962e33791c3ad6e04e50744172297cd59344c5_640.jpg");
				var userArticle2 = await db.CreateUserArticle(article2, user.Id, 400, true, new api.Analytics.ClientAnalytics());
				db.UpdateReadProgress(userArticle2.Id, new int[1] { 400 }, new api.Analytics.ClientAnalytics());

				await db.CreateComment("It was a decent read!", article2, null, user.Id, new api.Analytics.ClientAnalytics());

				// Run scoring
				// var cmd = new NpgsqlCommand("SELECT article_api.score_articles()", db);
				// await cmd.ExecuteNonQueryAsync();

				await db.ExecuteAsync(
					sql: "article_api.score_articles",
					commandType: CommandType.StoredProcedure
				);

				await db.SetAotd();

				// one ID gets returned now, for the "hot articles" db call, but is 
				// later processed by articles.get_articles which returns null 

				// SELECT articles.get_articles('{ 1, 2 }', 1) returned nothing
				// fixed by adding pages

				// SELECT community_reads.get_aotds(1) returns an id
				// SELECT articles.get_article_by_id(1, 1) also works

				// article 2 
				var article3 = db.CreateArticle(
						"Third best",
						"best-website-ever_article-three",
						sourceId: source.Id,
						datePublished: DateTime.Now.AddDays(-1),
						dateModified: null,
						section: "",
						description: "This might just be the best article ever",
						authors: new AuthorMetadata[1] { new AuthorMetadata("Mega Writer", "https://website.com/mega-writer", "mega-writer") },
						tags: new TagMetadata[0] { }
					);
				db.CreatePage(article3, 1, 400, 400, "https://website.com/article_three");
				await db.SetArticleImage(article3, user.Id, "https://randomwordgenerator.com/img/picture-generator/57e7d7444955ac14f1dc8460962e33791c3ad6e04e50744172297cd59344c5_640.jpg");
				var userArticle3 = await db.CreateUserArticle(article3, user.Id, 400, true, new api.Analytics.ClientAnalytics());
				db.UpdateReadProgress(userArticle3.Id, new int[1] { 400 }, new api.Analytics.ClientAnalytics());

				await db.CreateComment("It was a decent read!", article3, null, user.Id, new api.Analytics.ClientAnalytics());

				// set a second AOTD, so we get AOTD history for our homepage UI
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

		// TODO copy-pasted: make a public utility function in the API?
		private static string CreateSlug(string value)
		{
			var slug = Regex.Replace(Regex.Replace(value, @"[^a-zA-Z0-9-\s]", ""), @"\s", "-").ToLower();
			return slug.Length > 80 ? slug.Substring(0, 80) : slug;
		}
	}
}
