#!/bin/bash
set -e
apk add bash icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib
apk add libgdiplus --repository https://dl-3.alpinelinux.org/alpine/edge/testing/

cd /tmp
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 3.1

export DOTNET_ROOT=/root/.dotnet
/root/.dotnet/dotnet tool install -g marain

echo "TenancyClient__TenancyServiceBaseUri: $TenancyClient__TenancyServiceBaseUri"
echo "TenancyClient__ResourceIdForMsiAuthentication: $TenancyClient__ResourceIdForMsiAuthentication"
/root/.dotnet/tools/marain $@
