# FROM mcr.microsoft.com/dotnet/sdk:6.0 as builder
FROM mcr.microsoft.com/dotnet/sdk@sha256:ca4344774139fabfb58eed70381710c8912900d92cf879019d2eb52abc307102 as builder
COPY source/ /build/source
COPY references/ /build/references
COPY *.csproj /build/
WORKDIR /build
RUN dotnet build --configuration Release --output /output

# the final build is just an image that stores some files, so we use alpine since that is really tiny
FROM alpine:latest
COPY --from=builder /output/*.dll /nethermind/plugins/
COPY --from=builder /output/*.pdb /nethermind/plugins/
