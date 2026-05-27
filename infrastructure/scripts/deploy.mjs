#!/usr/bin/env node
// Deploys a Dokploy application with a specific image tag.
//
// Required env vars:
//   DOKPLOY_API_KEY        — API token from Dokploy dashboard → Settings → API
//   DOKPLOY_URL            — e.g. https://admin.mini-moeder.dk
//   DOKPLOY_APPLICATION_ID — application ID visible in Dokploy dashboard URL
//   IMAGE_TAG              — e.g. production, 1.25.1
//   IMAGE_NAME             — (optional) defaults to ghcr.io/nielspilgaard/minimoeder-website

const { DOKPLOY_API_KEY, DOKPLOY_URL, DOKPLOY_APPLICATION_ID, IMAGE_TAG } = process.env;

if (!DOKPLOY_API_KEY) throw new Error("DOKPLOY_API_KEY is not set");
if (!DOKPLOY_URL) throw new Error("DOKPLOY_URL is not set");
if (!DOKPLOY_APPLICATION_ID) throw new Error("DOKPLOY_APPLICATION_ID is not set");
if (!IMAGE_TAG) throw new Error("IMAGE_TAG is not set");

const IMAGE_NAME = process.env.IMAGE_NAME || "ghcr.io/nielspilgaard/minimoeder-website";
const headers = { "x-api-key": DOKPLOY_API_KEY, "Content-Type": "application/json" };

async function post(path, body) {
	const res = await fetch(`${DOKPLOY_URL}/api/${path}`, {
		method: "POST",
		headers,
		body: JSON.stringify(body),
	});
	if (!res.ok) throw new Error(`POST ${path} failed: ${res.status} ${await res.text()}`);
	return res;
}

async function get(path, query) {
	const qs = new URLSearchParams(query);
	const res = await fetch(`${DOKPLOY_URL}/api/${path}?${qs}`, { headers });
	if (!res.ok) throw new Error(`GET ${path} failed: ${res.status} ${await res.text()}`);
	return res.json();
}

async function updateImage() {
	await post("application.update", {
		applicationId: DOKPLOY_APPLICATION_ID,
		dockerImage: `${IMAGE_NAME}:${IMAGE_TAG}`,
		sourceType: "docker",
	});
	console.log(`Image updated to ${IMAGE_NAME}:${IMAGE_TAG}`);
}

async function deploy() {
	await post("application.deploy", { applicationId: DOKPLOY_APPLICATION_ID });
	console.log("Deploy triggered");
}

const MAX_WAIT_MS = 10 * 60 * 1000; // 10 minutes

async function waitForDeployment() {
	console.log("Polling deployment status...");
	const start = Date.now();
	for (;;) {
		if (Date.now() - start > MAX_WAIT_MS) {
			throw new Error(`Deployment timed out after ${MAX_WAIT_MS / 1000}s (applicationId: ${DOKPLOY_APPLICATION_ID})`);
		}
		const deployments = await get("deployment.allByApplication", { applicationId: DOKPLOY_APPLICATION_ID });
		if (!Array.isArray(deployments) || deployments.length === 0) {
			console.log(`  no deployments found for applicationId ${DOKPLOY_APPLICATION_ID}, retrying...`);
			await new Promise(r => setTimeout(r, 5000));
			continue;
		}
		const latest = deployments[0];
		console.log(`  status: ${latest.status}`);
		if (latest.status === "done") return;
		if (latest.status === "error") throw new Error(`Deployment failed: ${latest.errorMessage}`);
		if (latest.status === "cancelled") throw new Error("Deployment was cancelled");
		await new Promise(r => setTimeout(r, 5000));
	}
}

await updateImage();
await deploy();
await waitForDeployment();
console.log(`Deployed ${IMAGE_TAG} successfully`);
