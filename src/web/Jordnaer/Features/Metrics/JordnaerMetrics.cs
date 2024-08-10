﻿using System.Diagnostics.Metrics;
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
		Meter.CreateCounter<int>("jordnaer_emails_sent_total");
}