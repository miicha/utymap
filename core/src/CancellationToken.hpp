#ifndef CANCELLATIONTOKEN_HPP_DEFINED
#define CANCELLATIONTOKEN_HPP_DEFINED

namespace utymap {

/// Cancellation token.
struct CancellationToken final {
  /// Checks whether token is in cancelled state.
  bool isCancelled() const {
    return cancelled != 0;
  }

  /// Sets token into cancelled state.
  void cancel() {
    cancelled = 1;
  }

private:
  /// Non-zero value means cancellation.
  volatile int cancelled = 0;
};

}

#endif // CANCELLATIONTOKEN_HPP_DEFINED
