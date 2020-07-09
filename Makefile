all: pack

test:
	dotnet test src/Larva.MessageProcess.Tests

test-mq:
	dotnet test src/Larva.MessageProcess.RabbitMQ.Tests

publish: pack
	dotnet nuget push `pwd`/packages/Larva.MessageProcess.1.1.0.nupkg --source "github"

pack: build
	mkdir -p `pwd`/packages
	dotnet pack -c Release `pwd`/src/Larva.MessageProcess/
	mv `pwd`/src/Larva.MessageProcess/bin/Release/*.nupkg `pwd`/packages/

build:
	dotnet build -c Release `pwd`/src/Larva.MessageProcess/
	dotnet build -c Release `pwd`/src/Larva.MessageProcess.Tests/
	dotnet build -c Release `pwd`/src/Larva.MessageProcess.RabbitMQ/
	dotnet build -c Release `pwd`/src/Larva.MessageProcess.RabbitMQ.Tests/
