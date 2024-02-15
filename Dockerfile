FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:b246fc9a92feefe3aff49beb165fda4fc5a29122cd30768bafdc2d3e606c565c as builder
COPY source/ /build/source
COPY *.csproj /build/
WORKDIR /build
RUN dotnet build --configuration Release --output /output

# the final build is just an image that stores some files, so we use alpine since that is really tiny
FROM alpine:latest
COPY --from=builder /output/*.dll /nethermind/plugins/
COPY --from=builder /output/*.pdb /nethermind/plugins/
