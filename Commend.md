Make a tag:
git tag -a v0.1.1 -m "Comment"

git push origin v0.1.1 


Publish:
dotnet publish src/Phrazie.Desktop/Phrazie.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish/Phrazie-v0.1.0 &&
  Compress-Archive -Path publish/Phrazie-v0.1.0/* -DestinationPath publish/Phrazie-v0.x.x-win-x64.zip