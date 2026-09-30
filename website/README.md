# Website content

The guides and the scripting API reference shown on [qxscripter.xyz](https://qxscripter.xyz).

- `docs/` holds the guides. `docs/toc.yml` sets their order. QX Scripter embeds the same files for its MCP server.
- `api/index.md` is the front page of the API reference.
- The API reference itself comes from the XML documentation comments in `src`.

## Publishing

The Docs workflow runs on every push to `main` that touches `src` or `website`. It generates the API metadata with
DocFX, turns it and the guides into one bundle and publishes `manifest.json` and `docs.json.gz` to the `docs` branch
when the content changed. The site checks that branch on start and every 10 hours.

The bundle build fails on broken links, so a guide that links to a missing article, heading or type does not ship.

## Local build

```bash
dotnet tool install --global docfx --version 2.81.0
docfx metadata website/docfx.json
cd website
npm ci
npm run bundle
```

The bundle lands in `website/out`.
