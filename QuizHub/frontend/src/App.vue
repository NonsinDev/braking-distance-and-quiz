<script setup>
import { computed, ref } from 'vue'
import AdminView from './views/AdminView.vue'
import PlayerView from './views/PlayerView.vue'

const mode = ref(new URLSearchParams(window.location.search).get('mode') || '')
const isAdmin = computed(() => mode.value === 'admin')
</script>

<template>
  <main>
    <div v-if="!mode" class="mode-picker">
      <p class="eyebrow">FAHRSICHER / LIVE TRAINING</p>
      <h1>Verkehrssicherheit<br /><em>gemeinsam</em> erleben.</h1>
      <p class="intro">Interaktives Quiz für Polizei und Fahrsicherheitstraining.</p>
      <div class="mode-actions">
        <button class="primary-action" @click="mode = 'admin'">Quiz Hosten</button>
        <button class="secondary-action" @click="mode = 'player'">Quiz Beitreten</button>
      </div>
    </div>
    <AdminView v-else-if="isAdmin" />
    <PlayerView v-else />
  </main>
</template>
