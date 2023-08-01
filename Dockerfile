# FROM mcr.microsoft.com/dotnet/sdk:7.0 as builder
FROM mcr.microsoft.com/dotnet/sdk@sha256:e049e6a153619337ceb4edd040fb60a220d420414d41d6eb39708d6ce390bc7c as builder
COPY source/ /build/source
COPY *.csproj /build/
WORKDIR /build
RUN dotnet build --configuration Release --output /output

# the final build is just an image that stores some files, so we use alpine since that is really tiny
FROM alpine:latest
COPY --from=builder /output/*.dll /nethermind/plugins/
COPY --from=builder /output/*.pdb /nethermind/plugins/
