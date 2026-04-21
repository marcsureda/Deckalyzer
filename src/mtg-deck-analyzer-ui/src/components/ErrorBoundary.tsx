import { Component, type ErrorInfo, type ReactNode } from 'react';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

/**
 * Catches render-time errors from any child component tree.
 * Prevents the whole app from unmounting on an unexpected exception.
 */
export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('[ErrorBoundary]', error, info.componentStack);
  }

  private handleReset = () => this.setState({ hasError: false, error: null });

  render() {
    if (this.state.hasError) {
      return (
        this.props.fallback ?? (
          <div role="alert" style={{ padding: '2rem', textAlign: 'center' }}>
            <h2>Something went wrong</h2>
            <details style={{ marginBottom: '1rem' }}>
              <summary>Error details</summary>
              <pre style={{ textAlign: 'left', fontSize: '0.85rem' }}>
                {this.state.error?.message}
              </pre>
            </details>
            <button onClick={this.handleReset}>Try again</button>
          </div>
        )
      );
    }

    return this.props.children;
  }
}