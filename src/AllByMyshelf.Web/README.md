# AllByMyshelf.Web

Angular 21 frontend for All By Myshelf. See the [root README](../../README.md) for full setup instructions.

## Development server

```bash
npx ng serve --ssl
```

Runs at `https://localhost:4200`. Requires `src/environments/environment.ts` to exist — copy from the template:

```bash
cp src/environments/environment.template.ts src/environments/environment.ts
```

Then fill in your Auth0 credentials.

## Production build

```bash
npx ng build --configuration production
```

## Running tests

```bash
npx ng test
```
