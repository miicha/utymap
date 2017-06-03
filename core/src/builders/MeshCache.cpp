#include "builders/MeshCache.hpp"

#include "utils/CoreUtils.hpp"

using namespace utymap;
using namespace utymap::builders;

class MeshCache::MeshCacheImpl
{
public:
    explicit MeshCacheImpl(const std::string& directory) :
        directory_(directory)
    {
    }

    BuilderContext wrap(const BuilderContext& context)
    {
        // TODO
        return context;
    }

    bool fetch(const BuilderContext& context, const CancellationToken& cancelToken)
    {
        // TODO
        return false;
    }

    void release(const BuilderContext& context, const CancellationToken& cancelToken)
    {
        // TODO
    }

private:
    const std::string& directory_;
};

MeshCache::MeshCache(const std::string& directory) :
    pimpl_(utymap::utils::make_unique<MeshCacheImpl>(directory))
{
}

BuilderContext MeshCache::wrap(const BuilderContext& context) const
{
    return pimpl_->wrap(context);
}

bool MeshCache::fetch(const BuilderContext& context, const CancellationToken& cancelToken) const
{
    return pimpl_->fetch(context, cancelToken);
}

void MeshCache::release(const BuilderContext& context, const CancellationToken& cancelToken) const
{
    pimpl_->release(context, cancelToken);
}

MeshCache::~MeshCache()
{
}
