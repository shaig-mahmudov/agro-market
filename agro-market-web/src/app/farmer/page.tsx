"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { ApiError, productsApi } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import type { Product } from "@/lib/types";

export default function FarmerHubPage() {
  const { user } = useAuth();
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [category, setCategory] = useState("Tomatoes");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [availableStockKg, setAvailableStockKg] = useState("150");
  const [minOrderKg, setMinOrderKg] = useState("10");
  const [basePricePerKg, setBasePricePerKg] = useState("2.40");
  const [tierMinKg, setTierMinKg] = useState("50");
  const [tierPrice, setTierPrice] = useState("2.00");
  const [variety, setVariety] = useState("");
  const [grade, setGrade] = useState("A");
  const [packaging, setPackaging] = useState("10kg crate");
  const [isOrganic, setIsOrganic] = useState(false);
  const [creating, setCreating] = useState(false);

  const loadMine = useCallback(async () => {
    if (!user) return;
    setLoading(true);
    setError(null);
    try {
      const res = await productsApi.listMine();
      setProducts(res.items);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to load your listings.");
    } finally {
      setLoading(false);
    }
  }, [user]);

  useEffect(() => {
    if (!user) return;
    let active = true;
    productsApi
      .listMine()
      .then((res) => {
        if (!active) return;
        setProducts(res.items);
        setError(null);
      })
      .catch((err) => {
        if (!active) return;
        setError(err instanceof ApiError ? err.message : "Failed to load your listings.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [user]);

  if (!user || (user.role !== "Farmer" && user.role !== "Admin")) {
    return (
      <div className="mx-auto max-w-lg rounded-2xl border border-[#DCE6DF] bg-white p-6 text-center">
        <p className="font-semibold">Farmer Hub requires a Farmer or Admin account.</p>
        <Link
          href="/login"
          className="mt-4 inline-block rounded-xl bg-[#1B4332] px-4 py-2 text-sm font-semibold text-white"
        >
          Sign in
        </Link>
      </div>
    );
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setCreating(true);
    setError(null);
    try {
      const priceTiers =
        Number(tierMinKg) > 0 && Number(tierPrice) > 0
          ? [{ minQuantityKg: Number(tierMinKg), pricePerKg: Number(tierPrice) }]
          : [];

      await productsApi.create({
        category,
        title,
        description: description || undefined,
        availableStockKg: Number(availableStockKg),
        minOrderKg: Number(minOrderKg),
        basePricePerKg: Number(basePricePerKg),
        metadata: {
          variety: variety || null,
          grade: grade || null,
          packaging: packaging || null,
          isOrganic,
        },
        priceTiers,
      });

      setTitle("");
      setDescription("");
      await loadMine();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to publish listing.");
    } finally {
      setCreating(false);
    }
  }

  async function handleToggleActive(product: Product) {
    try {
      await productsApi.update(product.id, {
        isActive: !product.isActive,
        rowVersion: product.rowVersion,
      });
      await loadMine();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to update listing.");
    }
  }

  async function handleAddStock(product: Product, deltaKg: number) {
    try {
      await productsApi.update(product.id, {
        availableStockKg: Math.max(0, product.availableStockKg + deltaKg),
        rowVersion: product.rowVersion,
      });
      await loadMine();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to update stock.");
    }
  }

  async function handleDelete(productId: string) {
    try {
      await productsApi.remove(productId);
      await loadMine();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to delete listing.");
    }
  }

  return (
    <div className="space-y-6">
      {!user.isVerified && (
        <div className="rounded-2xl border border-amber-300 bg-amber-50 p-4 text-sm text-amber-900">
          Your farmer account is pending admin approval. Once verified, you can publish produce listings to restaurants.
        </div>
      )}

      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800">
          {error}
        </div>
      )}

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-12">
        {/* New Produce Form */}
        <section className="rounded-2xl border border-[#DCE6DF] bg-white p-5 shadow-xs lg:col-span-5">
          <h1 className="text-xl font-bold text-[#1B4332]">Publish Harvest Listing</h1>
          <form onSubmit={handleCreate} className="mt-4 space-y-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-semibold text-stone-700">Category</label>
                <input
                  type="text"
                  required
                  value={category}
                  onChange={(e) => setCategory(e.target.value)}
                  className="mt-1 w-full rounded-xl border border-[#D4DFD7] px-3 py-2 text-sm"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-stone-700">Variety</label>
                <input
                  type="text"
                  value={variety}
                  onChange={(e) => setVariety(e.target.value)}
                  placeholder="Pink Zira"
                  className="mt-1 w-full rounded-xl border border-[#D4DFD7] px-3 py-2 text-sm"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-stone-700">Listing Title *</label>
              <input
                type="text"
                required
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="e.g. Zira Pink Greenhouse Tomatoes"
                className="mt-1 w-full rounded-xl border border-[#D4DFD7] px-3 py-2 text-sm"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold text-stone-700">Description</label>
              <textarea
                rows={2}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                className="mt-1 w-full rounded-xl border border-[#D4DFD7] px-3 py-2 text-sm"
              />
            </div>

            <div className="grid grid-cols-3 gap-2">
              <div>
                <label className="block text-xs font-semibold text-stone-700">Stock (kg)</label>
                <input
                  type="number"
                  step="0.5"
                  required
                  value={availableStockKg}
                  onChange={(e) => setAvailableStockKg(e.target.value)}
                  className="mt-1 w-full rounded-xl border border-[#D4DFD7] px-2.5 py-2 text-sm"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-stone-700">Min (kg)</label>
                <input
                  type="number"
                  step="0.5"
                  required
                  value={minOrderKg}
                  onChange={(e) => setMinOrderKg(e.target.value)}
                  className="mt-1 w-full rounded-xl border border-[#D4DFD7] px-2.5 py-2 text-sm"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-stone-700">Base $/kg</label>
                <input
                  type="number"
                  step="0.01"
                  required
                  value={basePricePerKg}
                  onChange={(e) => setBasePricePerKg(e.target.value)}
                  className="mt-1 w-full rounded-xl border border-[#D4DFD7] px-2.5 py-2 text-sm"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3 rounded-xl bg-[#F7FAF8] p-3">
              <div>
                <label className="block text-xs font-semibold text-[#1B4332]">
                  Bulk Tier Min (kg)
                </label>
                <input
                  type="number"
                  value={tierMinKg}
                  onChange={(e) => setTierMinKg(e.target.value)}
                  className="mt-1 w-full rounded-lg border border-[#D4DFD7] bg-white px-2.5 py-1.5 text-sm"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-[#1B4332]">
                  Bulk Tier $/kg
                </label>
                <input
                  type="number"
                  step="0.01"
                  value={tierPrice}
                  onChange={(e) => setTierPrice(e.target.value)}
                  className="mt-1 w-full rounded-lg border border-[#D4DFD7] bg-white px-2.5 py-1.5 text-sm"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <input
                type="text"
                value={grade}
                onChange={(e) => setGrade(e.target.value)}
                placeholder="Grade (A)"
                className="rounded-xl border border-[#D4DFD7] px-3 py-2 text-sm"
              />
              <input
                type="text"
                value={packaging}
                onChange={(e) => setPackaging(e.target.value)}
                placeholder="10kg crate"
                className="rounded-xl border border-[#D4DFD7] px-3 py-2 text-sm"
              />
            </div>

            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={isOrganic}
                onChange={(e) => setIsOrganic(e.target.checked)}
                className="h-4 w-4 accent-[#1B4332]"
              />
              <span>Certified / Organic produce</span>
            </label>

            <button
              type="submit"
              disabled={creating || !user.isVerified}
              className="w-full rounded-xl bg-[#1B4332] py-2.5 text-sm font-semibold text-white hover:bg-[#2D6A4F] disabled:opacity-50"
            >
              {creating ? "Publishing..." : "Publish Listing"}
            </button>
          </form>
        </section>

        {/* Farmer Listings */}
        <section className="space-y-3 lg:col-span-7">
          <h2 className="text-xl font-bold text-[#1B4332]">Your Farm Inventory</h2>
          {loading ? (
            <div className="rounded-2xl border border-[#E2E8E4] bg-white p-6 text-sm text-stone-500">
              Loading your listings...
            </div>
          ) : products.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-[#C7D3CC] bg-white p-8 text-center text-sm text-stone-500">
              You have no active produce listings yet.
            </div>
          ) : (
            products.map((p) => (
              <div
                key={p.id}
                className="flex flex-wrap items-center justify-between gap-4 rounded-2xl border border-[#E2E8E4] bg-white p-4 shadow-xs"
              >
                <div>
                  <div className="flex items-center gap-2">
                    <span className="text-base font-bold text-[#1F2933]">{p.title}</span>
                    <span
                      className={`rounded-full px-2 py-0.5 text-[11px] font-semibold ${
                        p.isActive
                          ? "bg-emerald-100 text-emerald-800"
                          : "bg-stone-200 text-stone-600"
                      }`}
                    >
                      {p.isActive ? "Active" : "Paused"}
                    </span>
                  </div>
                  <p className="mt-0.5 text-xs text-stone-500">
                    {p.category} • Stock: <strong>{p.availableStockKg} kg</strong> • Base: $
                    {p.basePricePerKg.toFixed(2)}/kg
                  </p>
                </div>

                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => void handleAddStock(p, 25)}
                    className="rounded-lg border border-[#D4DFD7] px-2.5 py-1 text-xs font-semibold hover:bg-[#E9F5EE]"
                  >
                    +25 kg
                  </button>
                  <button
                    type="button"
                    onClick={() => void handleToggleActive(p)}
                    className="rounded-lg border border-[#D4DFD7] px-2.5 py-1 text-xs font-semibold hover:bg-stone-100"
                  >
                    {p.isActive ? "Pause" : "Activate"}
                  </button>
                  <button
                    type="button"
                    onClick={() => void handleDelete(p.id)}
                    className="rounded-lg border border-red-200 px-2.5 py-1 text-xs font-semibold text-red-700 hover:bg-red-50"
                  >
                    Delete
                  </button>
                </div>
              </div>
            ))
          )}
        </section>
      </div>
    </div>
  );
}
