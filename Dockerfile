##
## Build
##
FROM golang:1.18-buster AS build

WORKDIR /app

COPY go.mod ./
COPY go.sum ./
RUN go mod download

COPY *.go ./

RUN go build -o /picotorrent-http-api

##
## Deploy
##
FROM gcr.io/distroless/base-debian10

WORKDIR /

COPY --from=build /picotorrent-http-api /picotorrent-http-api

EXPOSE 3000

USER nonroot:nonroot

ENTRYPOINT ["/picotorrent-http-api"]
