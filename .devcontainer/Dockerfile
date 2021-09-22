FROM mcr.microsoft.com/dotnet/core/sdk:3.1

RUN apt update && apt -y install --no-install-recommends --no-install-suggests \
        bash-completion \
        dieharder \
        less \
        nano \
 && apt -y autoremove \
 && apt -y clean \
 && rm -rf /var/lib/apt/lists/*

RUN groupadd --system dev \
 && useradd --system --create-home --shell /bin/bash --gid dev dev \
 && mkdir -p /home/dev/.vscode-server/extensions \
 && mkdir -p /home/dev/.vscode-server/extensionsCache \
 && chown -R dev:dev /home/dev

USER dev

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
