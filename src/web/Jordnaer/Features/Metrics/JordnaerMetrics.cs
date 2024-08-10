using System.Diagnostics.Metrics;
using System.Reflection;

namespace Jordnaer.Features.Metrics;

internal static class JordnaerMetrics
{
	private static readonly AssemblyName AssemblyName = typeof(Program).Assembly.GetName();
	internal static readonly Meter Meter =
		new(name: AssemblyName.Name!,
			version: AssemblyName.Version?.ToString());

	internal static readonly Counter<int> ChatMessagesReceivedCounter =
		Meter.CreateCounter<int>("jordnaer_chat_messages_received_total");
	internal static readonly Counter<int> ChatMessagesSentCounter =
		Meter.CreateCounter<int>("jordnaer_chat_messages_sent_total");
	internal static readonly Counter<int> ChatStartedSentCounter =
		Meter.CreateCounter<int>("jordnaer_chat_chat_started_sent_total");
	internal static readonly Counter<int> ChatStartedReceivedCounter =
		Meter.CreateCounter<int>("jordnaer_chat_chat_started_received_total");

	internal static readonly Counter<int> EmailsSentCounter =
		Meter.CreateCounter<int>("jordnaer_email_emails_sent_total");

	internal static readonly Counter<int> LoginCounter =
		Meter.CreateCounter<int>("jordnaer_auth_login_total");
	internal static readonly Counter<int> ExternalLoginCounter =
		Meter.CreateCounter<int>("jordnaer_auth_external_login_total");
	internal static readonly Counter<int> FirstLoginCounter =
		Meter.CreateCounter<int>("jordnaer_auth_first_login_total");
	internal static readonly Counter<int> LogoutCounter =
		Meter.CreateCounter<int>("jordnaer_auth_logout_total");

	internal static readonly Counter<int> GroupsCreatedCounter =
		Meter.CreateCounter<int>("jordnaer_group_groups_created_total");

	internal static readonly Counter<int> GroupSearchesCounter =
		Meter.CreateCounter<int>("jordnaer_group_group_searches_total");

	internal static readonly Counter<int> UserSearchesCounter =
		Meter.CreateCounter<int>("jordnaer_user_user_searches_total");

	internal static readonly Counter<int> SponsorAdViewCounter =
		Meter.CreateCounter<int>("jordnaer_ad_sponsor_views_total");
}