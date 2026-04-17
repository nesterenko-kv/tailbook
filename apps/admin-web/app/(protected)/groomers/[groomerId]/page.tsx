"use client";

import { FormEvent, useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { apiRequest, ApiError } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import type {
  AvailabilityResult,
  GroomerDetail,
  GroomerScheduleResult,
  OfferListItem,
  PetCatalog
} from "@/lib/types";
import {
  Card,
  ErrorBanner,
  Field,
  Input,
  LinkButton,
  PageHeader,
  PrimaryButton,
  Select,
  SuccessBanner,
  TextArea
} from "@/components/ui";

function toLocalDateTimeInputValue(date: Date) {
  const offset = date.getTimezoneOffset();
  const local = new Date(date.getTime() - offset * 60_000);
  return local.toISOString().slice(0, 16);
}

export default function GroomerDetailPage() {
  const params = useParams<{ groomerId: string }>();
  const groomerId = String(params.groomerId);

  const [groomer, setGroomer] = useState<GroomerDetail | null>(null);
  const [catalog, setCatalog] = useState<PetCatalog | null>(null);
  const [offers, setOffers] = useState<OfferListItem[]>([]);
  const [scheduleView, setScheduleView] = useState<GroomerScheduleResult | null>(null);
  const [availability, setAvailability] = useState<AvailabilityResult | null>(null);

  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const [capabilityForm, setCapabilityForm] = useState({
    animalTypeId: "",
    breedId: "",
    breedGroupId: "",
    coatTypeId: "",
    sizeCategoryId: "",
    offerId: "",
    capabilityMode: "Allow",
    reservedDurationModifierMinutes: "0",
    notes: ""
  });

  const [scheduleForm, setScheduleForm] = useState({
    weekday: "1",
    startLocalTime: "09:00",
    endLocalTime: "18:00"
  });

  const [timeBlockForm, setTimeBlockForm] = useState({
    startAtUtc: "",
    endAtUtc: "",
    reasonCode: "TIME_OFF",
    notes: ""
  });

  const [scheduleQuery, setScheduleQuery] = useState({
    fromUtc: "",
    toUtc: ""
  });

  const [availabilityForm, setAvailabilityForm] = useState({
    startAtUtc: "",
    reservedMinutes: "120"
  });

  useEffect(() => {
    const now = new Date();
    const plusWeek = new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000);

    setScheduleQuery({
      fromUtc: toLocalDateTimeInputValue(now),
      toUtc: toLocalDateTimeInputValue(plusWeek)
    });

    setAvailabilityForm((current) => ({
      ...current,
      startAtUtc: current.startAtUtc || toLocalDateTimeInputValue(now)
    }));
  }, []);

  async function loadAll() {
    setError(null);

    try {
      const [groomerResponse, catalogResponse, offersResponse] = await Promise.all([
        apiRequest<GroomerDetail>(`/api/admin/groomers/${groomerId}`),
        apiRequest<PetCatalog>("/api/admin/pets/catalog"),
        apiRequest<OfferListItem[]>("/api/admin/catalog/offers")
      ]);

      setGroomer(groomerResponse);
      setCatalog(catalogResponse);
      setOffers(offersResponse);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to load groomer.");
    }
  }

  useEffect(() => {
    void loadAll();
  }, [groomerId]);

  async function addCapability(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    try {
      await apiRequest(`/api/admin/groomers/${groomerId}/capabilities`, {
        method: "POST",
        body: JSON.stringify({
          groomerId,
          animalTypeId: capabilityForm.animalTypeId || null,
          breedId: capabilityForm.breedId || null,
          breedGroupId: capabilityForm.breedGroupId || null,
          coatTypeId: capabilityForm.coatTypeId || null,
          sizeCategoryId: capabilityForm.sizeCategoryId || null,
          offerId: capabilityForm.offerId || null,
          capabilityMode: capabilityForm.capabilityMode,
          reservedDurationModifierMinutes: Number(capabilityForm.reservedDurationModifierMinutes),
          notes: capabilityForm.notes || null
        })
      });

      setSuccess("Capability added.");
      await loadAll();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to add capability.");
    }
  }

  async function upsertSchedule(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    try {
      await apiRequest(`/api/admin/groomers/${groomerId}/working-schedules`, {
        method: "POST",
        body: JSON.stringify({
          groomerId,
          weekday: Number(scheduleForm.weekday),
          startLocalTime: scheduleForm.startLocalTime,
          endLocalTime: scheduleForm.endLocalTime
        })
      });

      setSuccess("Working schedule saved.");
      await loadAll();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to save working schedule.");
    }
  }

  async function addTimeBlock(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    try {
      await apiRequest(`/api/admin/groomers/${groomerId}/time-blocks`, {
        method: "POST",
        body: JSON.stringify({
          groomerId,
          startAtUtc: new Date(timeBlockForm.startAtUtc).toISOString(),
          endAtUtc: new Date(timeBlockForm.endAtUtc).toISOString(),
          reasonCode: timeBlockForm.reasonCode,
          notes: timeBlockForm.notes || null
        })
      });

      setSuccess("Time block added.");
      await loadAll();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to add time block.");
    }
  }

  async function loadSchedule() {
    setError(null);

    try {
      const response = await apiRequest<GroomerScheduleResult>(
        `/api/admin/groomers/${groomerId}/schedule?fromUtc=${encodeURIComponent(
          new Date(scheduleQuery.fromUtc).toISOString()
        )}&toUtc=${encodeURIComponent(new Date(scheduleQuery.toUtc).toISOString())}`
      );

      setScheduleView(response);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to load schedule.");
    }
  }

  async function checkAvailability(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);

    try {
      const response = await apiRequest<AvailabilityResult>(
        `/api/admin/groomers/${groomerId}/availability/check`,
        {
          method: "POST",
          body: JSON.stringify({
            groomerId,
            startAtUtc: new Date(availabilityForm.startAtUtc).toISOString(),
            reservedMinutes: Number(availabilityForm.reservedMinutes)
          })
        }
      );

      setAvailability(response);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Failed to check availability.");
    }
  }

  return (
    <div className="flex flex-col gap-6 px-2 py-2">
      <PageHeader
        eyebrow="Staff detail"
        title={groomer?.displayName ?? "Groomer detail"}
        description="Manage capability rules, working schedules, blocked time, and availability checks."
        action={<LinkButton href="/groomers">Back to groomers</LinkButton>}
      />

      <ErrorBanner message={error} />
      <SuccessBanner message={success} />

      {!groomer ? (
        <div className="text-sm text-slate-300">Loading groomer…</div>
      ) : (
        <div className="grid gap-6 xl:grid-cols-2">
          <Card title="Capabilities">
            <form className="grid gap-4 md:grid-cols-2" onSubmit={addCapability}>
              <Field label="Mode">
                <Select
                  value={capabilityForm.capabilityMode}
                  onChange={(e) =>
                    setCapabilityForm((current) => ({
                      ...current,
                      capabilityMode: e.target.value
                    }))
                  }
                >
                  <option>Allow</option>
                  <option>Deny</option>
                </Select>
              </Field>

              <Field label="Reserved modifier (minutes)">
                <Input
                  type="number"
                  value={capabilityForm.reservedDurationModifierMinutes}
                  onChange={(e) =>
                    setCapabilityForm((current) => ({
                      ...current,
                      reservedDurationModifierMinutes: e.target.value
                    }))
                  }
                />
              </Field>

              <Field label="Animal type">
                <Select
                  value={capabilityForm.animalTypeId}
                  onChange={(e) =>
                    setCapabilityForm((current) => ({
                      ...current,
                      animalTypeId: e.target.value
                    }))
                  }
                >
                  <option value="">Any</option>
                  {catalog?.animalTypes.map((x) => (
                    <option key={x.id} value={x.id}>
                      {x.name}
                    </option>
                  ))}
                </Select>
              </Field>

              <Field label="Breed">
                <Select
                  value={capabilityForm.breedId}
                  onChange={(e) =>
                    setCapabilityForm((current) => ({
                      ...current,
                      breedId: e.target.value
                    }))
                  }
                >
                  <option value="">Any</option>
                  {catalog?.breeds.map((x) => (
                    <option key={x.id} value={x.id}>
                      {x.name}
                    </option>
                  ))}
                </Select>
              </Field>

              <Field label="Breed group">
                <Select
                  value={capabilityForm.breedGroupId}
                  onChange={(e) =>
                    setCapabilityForm((current) => ({
                      ...current,
                      breedGroupId: e.target.value
                    }))
                  }
                >
                  <option value="">Any</option>
                  {catalog?.breedGroups.map((x) => (
                    <option key={x.id} value={x.id}>
                      {x.name}
                    </option>
                  ))}
                </Select>
              </Field>

              <Field label="Coat type">
                <Select
                  value={capabilityForm.coatTypeId}
                  onChange={(e) =>
                    setCapabilityForm((current) => ({
                      ...current,
                      coatTypeId: e.target.value
                    }))
                  }
                >
                  <option value="">Any</option>
                  {catalog?.coatTypes.map((x) => (
                    <option key={x.id} value={x.id}>
                      {x.name}
                    </option>
                  ))}
                </Select>
              </Field>

              <Field label="Size category">
                <Select
                  value={capabilityForm.sizeCategoryId}
                  onChange={(e) =>
                    setCapabilityForm((current) => ({
                      ...current,
                      sizeCategoryId: e.target.value
                    }))
                  }
                >
                  <option value="">Any</option>
                  {catalog?.sizeCategories.map((x) => (
                    <option key={x.id} value={x.id}>
                      {x.name}
                    </option>
                  ))}
                </Select>
              </Field>

              <Field label="Offer">
                <Select
                  value={capabilityForm.offerId}
                  onChange={(e) =>
                    setCapabilityForm((current) => ({
                      ...current,
                      offerId: e.target.value
                    }))
                  }
                >
                  <option value="">Any</option>
                  {offers.map((x) => (
                    <option key={x.id} value={x.id}>
                      {x.displayName}
                    </option>
                  ))}
                </Select>
              </Field>

              <Field label="Notes" className="md:col-span-2">
                <TextArea
                  value={capabilityForm.notes}
                  onChange={(e) =>
                    setCapabilityForm((current) => ({
                      ...current,
                      notes: e.target.value
                    }))
                  }
                />
              </Field>

              <PrimaryButton type="submit" className="md:col-span-2">
                Add capability
              </PrimaryButton>
            </form>

            <div className="mt-4 grid gap-3">
              {groomer.capabilities.map((item) => (
                <div
                  key={item.id}
                  className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-sm"
                >
                  <div className="font-medium">{item.capabilityMode}</div>
                  <div className="text-slate-400">
                    Modifier: {item.reservedDurationModifierMinutes} min
                  </div>
                  <div className="text-slate-400">
                    Created {formatDateTime(item.createdAtUtc)}
                  </div>
                </div>
              ))}
            </div>
          </Card>

          <Card title="Working schedule and blocks">
            <form className="grid gap-4 md:grid-cols-3" onSubmit={upsertSchedule}>
              <Field label="Weekday">
                <Select
                  value={scheduleForm.weekday}
                  onChange={(e) =>
                    setScheduleForm((current) => ({
                      ...current,
                      weekday: e.target.value
                    }))
                  }
                >
                  {[0, 1, 2, 3, 4, 5, 6].map((x) => (
                    <option key={x} value={x}>
                      {x}
                    </option>
                  ))}
                </Select>
              </Field>

              <Field label="Start">
                <Input
                  type="time"
                  value={scheduleForm.startLocalTime}
                  onChange={(e) =>
                    setScheduleForm((current) => ({
                      ...current,
                      startLocalTime: e.target.value
                    }))
                  }
                />
              </Field>

              <Field label="End">
                <Input
                  type="time"
                  value={scheduleForm.endLocalTime}
                  onChange={(e) =>
                    setScheduleForm((current) => ({
                      ...current,
                      endLocalTime: e.target.value
                    }))
                  }
                />
              </Field>

              <PrimaryButton type="submit" className="md:col-span-3">
                Save working schedule
              </PrimaryButton>
            </form>

            <form className="mt-6 grid gap-4 md:grid-cols-2" onSubmit={addTimeBlock}>
              <Field label="Block from">
                <Input
                  type="datetime-local"
                  value={timeBlockForm.startAtUtc}
                  onChange={(e) =>
                    setTimeBlockForm((current) => ({
                      ...current,
                      startAtUtc: e.target.value
                    }))
                  }
                  required
                />
              </Field>

              <Field label="Block to">
                <Input
                  type="datetime-local"
                  value={timeBlockForm.endAtUtc}
                  onChange={(e) =>
                    setTimeBlockForm((current) => ({
                      ...current,
                      endAtUtc: e.target.value
                    }))
                  }
                  required
                />
              </Field>

              <Field label="Reason code">
                <Input
                  value={timeBlockForm.reasonCode}
                  onChange={(e) =>
                    setTimeBlockForm((current) => ({
                      ...current,
                      reasonCode: e.target.value
                    }))
                  }
                  required
                />
              </Field>

              <Field label="Notes">
                <TextArea
                  value={timeBlockForm.notes}
                  onChange={(e) =>
                    setTimeBlockForm((current) => ({
                      ...current,
                      notes: e.target.value
                    }))
                  }
                />
              </Field>

              <PrimaryButton type="submit" className="md:col-span-2">
                Add time block
              </PrimaryButton>
            </form>
          </Card>

          <Card title="Schedule view">
            <div className="grid gap-4 md:grid-cols-2">
              <Field label="From">
                <Input
                  type="datetime-local"
                  value={scheduleQuery.fromUtc}
                  onChange={(e) =>
                    setScheduleQuery((current) => ({
                      ...current,
                      fromUtc: e.target.value
                    }))
                  }
                />
              </Field>

              <Field label="To">
                <Input
                  type="datetime-local"
                  value={scheduleQuery.toUtc}
                  onChange={(e) =>
                    setScheduleQuery((current) => ({
                      ...current,
                      toUtc: e.target.value
                    }))
                  }
                />
              </Field>

              <PrimaryButton
                type="button"
                className="md:col-span-2"
                onClick={() => void loadSchedule()}
              >
                Load schedule
              </PrimaryButton>
            </div>

            {scheduleView ? (
              <div className="mt-4 space-y-3 text-sm">
                <div>Available windows: {scheduleView.availabilityWindows.length}</div>

                {scheduleView.availabilityWindows.map((x, i) => (
                  <div
                    key={i}
                    className="rounded-2xl border border-slate-800 bg-slate-950/60 p-3"
                  >
                    {formatDateTime(x.startAtUtc)} → {formatDateTime(x.endAtUtc)}
                  </div>
                ))}
              </div>
            ) : null}
            
          </Card>

          <Card title="Availability check">
            <form className="grid gap-4 md:grid-cols-2" onSubmit={checkAvailability}>
              <Field label="Start">
                <Input
                  type="datetime-local"
                  value={availabilityForm.startAtUtc}
                  onChange={(e) =>
                    setAvailabilityForm((current) => ({
                      ...current,
                      startAtUtc: e.target.value
                    }))
                  }
                />
              </Field>

              <Field label="Reserved minutes">
                <Input
                  type="number"
                  value={availabilityForm.reservedMinutes}
                  onChange={(e) =>
                    setAvailabilityForm((current) => ({
                      ...current,
                      reservedMinutes: e.target.value
                    }))
                  }
                />
              </Field>

              <PrimaryButton type="submit" className="md:col-span-2">
                Check availability
              </PrimaryButton>
            </form>

            {availability ? (
              <div className="mt-4 rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-sm">
                <div className="font-medium">
                  {availability.isAvailable ? "Available" : "Unavailable"}
                </div>
                <div className="text-slate-400">
                  End: {formatDateTime(availability.endAtUtc)}
                </div>
                <div className="text-slate-400">
                  Reserved: {availability.checkedReservedMinutes} min
                </div>
                {availability.reasons.map((reason, index) => (
                  <div key={index} className="text-slate-300">
                    • {reason}
                  </div>
                ))}
              </div>
            ) : null}
          </Card>
        </div>
      )}
    </div>
  );
}
