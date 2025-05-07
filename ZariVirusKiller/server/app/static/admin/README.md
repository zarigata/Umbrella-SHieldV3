# ZariVirusKiller Admin Dashboard

This is the admin dashboard for the ZariVirusKiller antivirus solution. It provides a web interface for managing licenses, virus signatures, and definition updates.

## Features

- License management (create, view, track)
- Virus signature management
- Definition updates tracking
- User authentication and authorization

## Technology Stack

- TypeScript for type-safe JavaScript
- Tailwind CSS for styling
- Chart.js for data visualization
- Fetch API for backend communication

## Setup and Development

### Prerequisites

- Node.js (v14 or later)
- npm (v6 or later)

### Installation

1. Navigate to the admin dashboard directory:
   ```
   cd server/app/static/admin
   ```

2. Install dependencies:
   ```
   npm install
   ```

3. Build the project:
   ```
   npm run build
   ```

### Development

To start the development server with hot reloading:

```
npm run dev
```

This will watch for changes in TypeScript files and CSS, and rebuild automatically.

## Project Structure

```
/admin
  /src              # TypeScript source files
    /styles         # CSS styles
      input.css     # Tailwind input file
    index.ts        # Main TypeScript file
  /dist             # Compiled output (generated)
  index.html        # Main HTML file
  tsconfig.json     # TypeScript configuration
  tailwind.config.js # Tailwind CSS configuration
  package.json      # npm dependencies and scripts
```

## API Integration

The dashboard communicates with the ZariVirusKiller server API for all operations. The API endpoints are defined in the `ApiService` class in `src/index.ts`.

## Authentication

The dashboard uses JWT (JSON Web Token) for authentication. The token is stored in localStorage and sent with each API request in the Authorization header.

## Building for Production

To build the dashboard for production:

```
npm run build
```

This will generate optimized JavaScript and CSS files in the `dist` directory.