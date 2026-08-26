FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/GroupDocs.Comparison.Mcp/GroupDocs.Comparison.Mcp.csproj \
    -c Release -o /app --no-self-contained

FROM base AS final

# Native dependencies the comparison engine needs on Linux.
# ttf-mscorefonts-installer supplies Arial / Times New Roman / etc. — required
# for faithful glyph rendering, because `compare` produces marked-up output that
# re-renders text glyphs and font resolution fails without the MS core fonts.
# The EULA is pre-accepted via debconf, and fc-cache rebuilds the font index
# after install. libfontconfig1 is the font-configuration library those lookups
# go through.
USER root
RUN apt-get update \
 && echo "ttf-mscorefonts-installer msttcorefonts/accepted-mscorefonts-eula select true" | debconf-set-selections \
 && apt-get install -y --no-install-recommends libgdiplus libfontconfig1 ttf-mscorefonts-installer \
 && fc-cache -f -v \
 && rm -rf /var/lib/apt/lists/*

# OCI image labels. VERSION is supplied by the publish workflow via
# --build-arg VERSION=YY.MM.N. Default 'dev' makes local `docker build` runs
# self-identify instead of inheriting the base image's misleading version.
ARG VERSION=dev
LABEL org.opencontainers.image.title="GroupDocs.Comparison.Mcp" \
      org.opencontainers.image.description="MCP server exposing GroupDocs.Comparison for .NET as AI-callable tools (Compare, GetDocumentInfo) for Claude, Cursor, GitHub Copilot, and other MCP agents." \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.source="https://github.com/groupdocs-comparison/GroupDocs.Comparison.Mcp" \
      org.opencontainers.image.url="https://www.nuget.org/packages/GroupDocs.Comparison.Mcp" \
      org.opencontainers.image.licenses="MIT" \
      org.opencontainers.image.vendor="Aspose Pty Ltd"

COPY --from=build /app .

ENV GROUPDOCS_MCP_STORAGE_PATH=/data
VOLUME /data

USER $APP_UID

ENTRYPOINT ["dotnet", "GroupDocs.Comparison.Mcp.dll"]
