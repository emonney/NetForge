// RFC 7807 ProblemDetails as the backend emits it (see Platform/Errors/GlobalExceptionHandler).
export interface ProblemDetails {
  type?: string;
  title?: string;
  status: number;
  detail?: string;
  instance?: string;
  code?: string;
  traceId?: string;
  /** Per-field validation messages: field name → messages. */
  errors?: Record<string, string[]>;
}

/** Thrown by the API client for any non-2xx response, carrying the parsed ProblemDetails. */
export class ApiError extends Error {
  readonly problem: ProblemDetails;

  constructor(problem: ProblemDetails) {
    super(problem.detail || problem.title || 'Something went wrong.');
    this.name = 'ApiError';
    this.problem = problem;
  }

  get status(): number {
    return this.problem.status;
  }

  /** Stable machine code (e.g. INVALID_CREDENTIALS) for branching without parsing messages. */
  get code(): string | undefined {
    return this.problem.code;
  }

  get traceId(): string | undefined {
    return this.problem.traceId;
  }

  get fieldErrors(): Record<string, string[]> | undefined {
    return this.problem.errors;
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}
