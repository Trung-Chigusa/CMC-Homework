# Bài 4 - CI/CD Output

Workflow file:

```text
.github/workflows/ci.yml
```

Jobs:

- Backend restore/build/test.
- Frontend install/build.
- Trivy filesystem security scan.

Local verification before push:

```text
dotnet test CMC-Homework.sln
npm run build
```

Note: after opening a pull request, GitHub Actions should show the workflow checks on the PR page.
