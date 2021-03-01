ARG IMAGE_TAG=5.0-alpine3.13
FROM mcr.microsoft.com/dotnet/sdk:${IMAGE_TAG} AS build-env
ARG BUILD_ID=local
ARG COMMIT_ID=dirty
ARG RUNTIME_ID=alpine-x64

COPY ./src /src
RUN dotnet publish \
    --configuration Release \
    --output /dist \
    --runtime ${RUNTIME_ID} \
    -p:BuildId=${BUILD_ID} \
    -p:SourceRevisionId=${COMMIT_ID} \
    -p:PublishTrimmed=True \
    src/Pokespeare

FROM mcr.microsoft.com/dotnet/runtime-deps:${IMAGE_TAG}
COPY --from=build-env /dist /app
EXPOSE 80
ENTRYPOINT [ "/app/Pokespeare" ]
